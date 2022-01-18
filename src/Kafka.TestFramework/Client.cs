using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Kafka.Protocol;
using Int32 = Kafka.Protocol.Int32;

namespace Kafka.TestFramework
{
    internal abstract class Client : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        private readonly Pipe _pipe = new Pipe();
        private readonly INetworkClient _networkClient;
        private Task _sendAndReceiveBackgroundTask = default!;

        protected Client(INetworkClient networkClient)
        {
            _networkClient = networkClient;
            NetworkClient = new NetworkStream(networkClient);
            Reader = _pipe.Reader;
        }

        protected NetworkStream NetworkClient { get; }
        protected PipeReader Reader { get; }
        
        protected void StartReceiving()
        {
            _sendAndReceiveBackgroundTask = Task.Run(
                async () =>
                {
                    var cancellationToken = _cancellationSource.Token;
                    try
                    {
                        var dataReceiver = new DataReceiver(_networkClient);
                        while (cancellationToken.IsCancellationRequested == false)
                        {
                            await dataReceiver
                                .ReceiveAsync(_pipe.Writer, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                    catch when (_cancellationSource.IsCancellationRequested)
                    {
                        // Shutdown in progress
                    }
                });
        }

        public async ValueTask DisposeAsync()
        {
            _cancellationSource.Cancel();

            await _sendAndReceiveBackgroundTask
                .ConfigureAwait(false);
        }
    }
}