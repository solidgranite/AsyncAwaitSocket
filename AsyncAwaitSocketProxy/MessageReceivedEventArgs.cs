using System;

namespace AsyncAwaitSocketProxy
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}