// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using Naninovel.Bridging;
using Naninovel.WebSocketSharp.Server;

namespace Naninovel
{
    public class BridgingListener : IServerTransport
    {
        public bool Listening => server?.IsListening ?? false;

        private readonly AsyncQueue<BridgingSocket> sockets = new AsyncQueue<BridgingSocket>();
        private WebSocketServer server;
        private CancellationTokenSource cts;

        public void StartListening (int port)
        {
            StopListening();
            cts = new CancellationTokenSource();
            server = new WebSocketServer(port);
            server.Start();
            server.AddWebSocketService<BridgingSocket>("/", sockets.Enqueue);
        }

        public void StopListening ()
        {
            server?.Stop();
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        public async Task<ITransport> WaitConnectionAsync (CancellationToken token)
        {
            token.Register(cts.Cancel);
            return await sockets.WaitAsync(cts.Token);
        }

        public void Dispose ()
        {
            server?.Stop();
            cts?.Dispose();
            sockets.Dispose();
        }
    }
}
