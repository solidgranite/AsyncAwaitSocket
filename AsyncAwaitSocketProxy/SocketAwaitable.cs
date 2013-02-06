// http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwaitSocketProxy
{
    public sealed class SocketAwaitable : INotifyCompletion
    {
        private readonly static Action Sentinel = () => { };

        internal bool WasCompleted;
        internal Action Continuation;
        internal SocketAsyncEventArgs EventArgs;

        public SocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null) throw new ArgumentNullException("eventArgs");
            EventArgs = eventArgs;
            eventArgs.Completed += delegate
                {
                    var prev = Continuation ?? Interlocked.CompareExchange(
                        ref Continuation, Sentinel, null);
                    if (prev != null) prev();
                };
        }

        internal void Reset()
        {
            WasCompleted = false;
            Continuation = null;
        }

        public SocketAwaitable GetAwaiter() { return this; }

        public bool IsCompleted { get { return WasCompleted; } }

        public void OnCompleted(Action continuation)
        {
            if (Continuation == Sentinel ||
                Interlocked.CompareExchange(
                    ref Continuation, continuation, null) == Sentinel)
            {
                Task.Run(continuation);
            }
        }

        public void GetResult()
        {
            if (EventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)EventArgs.SocketError);
        }
    }
}