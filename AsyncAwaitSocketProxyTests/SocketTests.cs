using AsyncAwaitSocketProxy;
using NUnit.Framework;

namespace AsyncAwaitSocketProxyTests
{
    public class SocketTests
    {
        private const byte StartFrame = 0x0b;
        private const byte EndFrame = 0x1c;
        private InboundSocketProxy _server;

        [TestFixtureSetUp]
        public async void StartListening()
        {
            var framingProtocol = new FramingProtocol(StartFrame, EndFrame);
            var logger = new ConsoleLogger();
            var endPoint = Network.GetLocalEndPoint(7777);

            _server = new InboundSocketProxy(endPoint, logger, framingProtocol);
            _server.MessageReceived += (sender, args) => _server.SendFramedData("Hello World");

            await _server.StartListening();

        }

        [Test]
        public async void CanSendAndReceiveData()
        {
            var framingProtocol = new FramingProtocol(StartFrame, EndFrame);
            var logger = new ConsoleLogger();
            var endPoint = Network.GetLocalEndPoint(7777);

            var client = new OutboundSocketProxy(endPoint, logger, framingProtocol);
            client.MessageReceived += (sender, args) => Assert.AreEqual("Hello World", args.Message);
            await client.Send("Hello World");

        }

    }
}
