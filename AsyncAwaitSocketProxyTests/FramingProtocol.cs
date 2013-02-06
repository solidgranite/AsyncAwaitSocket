using System.Globalization;
using System.Linq;
using AsyncAwaitSocketProxy;

namespace AsyncAwaitSocketProxyTests
{
    public class FramingProtocol : IFramingProtocol
    {
        public FramingProtocol(byte startFrame, byte endFrame)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
        }

        public byte StartFrame { get; set; }
        public byte EndFrame { get; set; }

        public byte[] FrameMessage(byte[] messageBytes)
        {
            var initialBytes = new[] { StartFrame };
            var finalBytes = new [] { EndFrame };
            var msg = initialBytes
                .Concat(messageBytes)
                .Concat(finalBytes)
                .ToArray();
            return msg;
        }

        public string UnframeMessage(string message)
        {
            if (message.StartsWith(((char) StartFrame).ToString(CultureInfo.InvariantCulture)))
            {
                message = message.Remove(0, 1);
            }

            if (message.EndsWith(((char)EndFrame).ToString(CultureInfo.InvariantCulture)))
            {
                message = message.Remove(message.Length - 1);
            }
            return message;
        }
    }
}
