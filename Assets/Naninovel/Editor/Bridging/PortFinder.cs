// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Net;
using System.Net.Sockets;

namespace Naninovel
{
    public static class PortFinder
    {
        public static bool IsPortAvailable (int port, int timeout = 500)
        {
            try { return IsTcpPortAvailable(port, timeout); }
            catch { return true; }
        }

        private static bool IsTcpPortAvailable (int port, int timeout = 500)
        {
            var client = new TcpClient();
            var result = client.BeginConnect(IPAddress.Loopback, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout));
            if (!success) return true;
            client.EndConnect(result);
            client.Dispose();
            return false;
        }
    }
}
