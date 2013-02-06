using System.Net.Sockets;
using System.Text;

namespace AsyncAwaitSocketProxy
{
    public class StateObject
    {
        public Socket WorkSocket = null;
        public const int BufferSize = 1024;
        public byte[] Buffer = new byte[BufferSize];
        private readonly StringBuilder _sb = new StringBuilder();
        public bool StartedReceiving;

        public void Append(string value)
        {
            _sb.Append(value);
            StartedReceiving = true;
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
