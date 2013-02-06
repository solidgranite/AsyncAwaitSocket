using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace AsyncAwaitSocketProxyTests
{
    public static class Network
    {
        public static IPEndPoint GetLocalEndPoint(int portNumber)
        {
            var ipAddress = GetLocalIpAddress();
            var localEndPoint = new IPEndPoint(ipAddress, portNumber);
            return localEndPoint;
        }

        public static IPAddress GetLocalIpAddress()
        {
            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = ipHostInfo.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
            return ipAddress;
        }

    }
}