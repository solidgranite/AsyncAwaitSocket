using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncAwaitSocketProxy
{
    public class OutboundSocketProxy : IDisposable
    {
        private readonly IPEndPoint _endpoint;
        private readonly IFramingProtocol _framingProtocol;
        private ILogger _logger;
        private Socket _socket;
        private bool _isDisposed;
        public EventHandler<MessageReceivedEventArgs> MessageReceived;

        public OutboundSocketProxy(IPEndPoint endpoint, ILogger logger, IFramingProtocol framingProtocol)
        {
            _endpoint = endpoint;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _logger = logger;
            _framingProtocol = framingProtocol;
        }

        public async Task Send(string message)
        {
            var state = new ClientStateObject { WorkSocket = _socket, OutboundMessage = message };
            if (_socket.Connected)
                await StartSending(state);
            else
            {
                _logger.Info(String.Format("Connecting to endpoint {0}", _endpoint));
                await StartConnecting(state);
            }
        }

        private async Task StartConnecting(ClientStateObject state)
        {
            var args = new SocketAsyncEventArgs() { RemoteEndPoint = _endpoint };
            var awaitable = new SocketAwaitable(args);
            await state.WorkSocket.ConnectAsync(awaitable);
            await StartSending(state);
        }

        private async Task StartSending(ClientStateObject state)
        {
            var byteData = Encoding.ASCII.GetBytes(state.OutboundMessage);
            var framedData = _framingProtocol.FrameMessage(byteData);
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(framedData, 0, framedData.Length);
            var awaitable = new SocketAwaitable(args);
            await state.WorkSocket.SendAsync(awaitable);

            // complete sending to remote endpoint
            var bytesSent = args.BytesTransferred;
            _logger.Info(string.Format("Sent {0} bytes to server", bytesSent));
            await StartReceiving(state);

        }

        private async Task StartReceiving(ClientStateObject state)
        {
            if (_isDisposed) return;
            if (!_socket.Connected) return;

            _logger.Info("Receiving message...");
            try
            {
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(new byte[0x1000], 0, 0x1000);
                var awaitable = new SocketAwaitable(args);

                while (true)
                {
                    await _socket.ReceiveAsync(awaitable);
                    var bytesRead = args.BytesTransferred;
                    if (bytesRead <= 0) break;

                    _logger.Info(string.Format("Bytes Read: {0}", bytesRead));
                    if (awaitable.EventArgs.Buffer[0] == _framingProtocol.StartFrame || state.StartedReceiving)
                    {
                        state.Append(Encoding.ASCII.GetString(awaitable.EventArgs.Buffer, 0, bytesRead));
                    }

                    if (awaitable.EventArgs.Buffer[bytesRead - 1] == _framingProtocol.EndFrame) // We're done
                    {
                        InvokeMessageReceived(_framingProtocol.UnframeMessage(state.ToString()));
                    }
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.ConnectionAborted) throw;
                // reconnect and send again
                _socket.Close();
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Send(state.OutboundMessage);
            }
        }

        private void InvokeMessageReceived(string message)
        {
            if (MessageReceived != null)
                MessageReceived.Invoke(this, new MessageReceivedEventArgs { Message = message });
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            if (_socket != null) _socket.Dispose();
        }
    }
}
