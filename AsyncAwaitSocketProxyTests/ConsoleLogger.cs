using System;
using AsyncAwaitSocketProxy;

namespace AsyncAwaitSocketProxyTests
{
    class ConsoleLogger : ILogger
    {
        public void Info(string infoMessage)
        {
            Console.WriteLine(infoMessage);
        }
    }
}
