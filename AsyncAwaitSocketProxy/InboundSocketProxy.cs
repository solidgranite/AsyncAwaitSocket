using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncAwaitSocketProxy
{
    public class InboundSocketProxy : IDisposable
    {
        private readonly IPEndPoint _endpoint;
        private readonly ILogger _logger;
        private readonly IFramingProtocol _framingProtocol;
        private readonly Socket _listener;
        private Socket _socket;
        private bool _isDisposed;
        public EventHandler<MessageReceivedEventArgs> MessageReceived;

        public InboundSocketProxy(IPEndPoint endpoint, ILogger logger, IFramingProtocol framingProtocol)
        {
            _endpoint = endpoint;
            _framingProtocol = framingProtocol;
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _logger = logger;
        }

        public async Task StartListening()
        {
            _listener.Bind(_endpoint);
            _listener.Listen(100);
            _logger.Info(string.Format("Started listening for connections @ {0} on port {1}...", _endpoint.Address, _endpoint.Port.ToString(CultureInfo.InvariantCulture)));

            await StartAccepting();
        }

        private async Task StartAccepting()
        {
            _logger.Info("Started accepting connections...");

            var args = new SocketAsyncEventArgs();
            var awaitable = new SocketAwaitable(args);
            await _listener.AcceptAsync(awaitable);

            if (_isDisposed) return;

            _socket = args.AcceptSocket;
            _logger.Info("Connection accepted.");
            await StartReceiving(new StateObject { WorkSocket = _socket });

        }

        private async Task StartReceiving(StateObject state)
        {
            if (_isDisposed) return;
            if (!_socket.Connected) return;

            _logger.Info("Receiving message...");

            var args = new SocketAsyncEventArgs();
            args.SetBuffer(new byte[0x1000], 0, 0x1000);
            var awaitable = new SocketAwaitable(args);

            while (true)
            {
                await _socket.ReceiveAsync(awaitable);
                var bytesRead = args.BytesTransferred;
                if (bytesRead <= 0) break;

                _logger.Info(string.Format("Bytes read: {0}", bytesRead));
                if (awaitable.EventArgs.Buffer[0] == _framingProtocol.StartFrame || state.StartedReceiving)
                {
                    state.Append(Encoding.ASCII.GetString(awaitable.EventArgs.Buffer, 0, bytesRead));
                }

                if (awaitable.EventArgs.Buffer[bytesRead - 1] == _framingProtocol.EndFrame) // We're done
                {
                    InvokeMessageReceived(state.ToString());
                }
            }
        }

        private void InvokeMessageReceived(string message)
        {
            if (MessageReceived != null)
                MessageReceived.Invoke(this, new MessageReceivedEventArgs{ Message = message});
        }

        public async Task SendFramedData(string message)
        {
            var msg = Encoding.UTF8.GetBytes(message);
            var framedMsg = _framingProtocol.FrameMessage(msg);
            if (_socket != null)
            {
                var args = new SocketAsyncEventArgs() { };
                args.SetBuffer(framedMsg, 0, framedMsg.Length);
                var awaitable = new SocketAwaitable(args);
                await _socket.SendAsync(awaitable);

                _logger.Info("Framed message sent.");
                _socket.Shutdown(SocketShutdown.Both);
                _logger.Info("Disconnecting from system...");

                await _socket.DisconnectAsync(awaitable);
                _logger.Info("System disconnected.");
                await StartAccepting();

            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            if (_listener != null) _listener.Dispose();
            if (_socket != null) _socket.Dispose();
        }
    }
}