using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kafka.TestFramework
{
    internal abstract class Client<TSendPayload> : IAsyncDisposable
        where TSendPayload : IPayload
    {
        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        private readonly Pipe _pipe = new Pipe();
        private readonly INetworkClient _networkClient;
        private Task _sendAndReceiveBackgroundTask = default!;

        protected Client(INetworkClient networkClient)
        {
            _networkClient = networkClient;
            Reader = new KafkaReader(_pipe.Reader);
        }

        protected IKafkaReader Reader { get; }

        public async ValueTask SendAsync(
            TSendPayload payload,
            CancellationToken cancellationToken = default)
        {
            var buffer = new MemoryStream();
            await using var _ = buffer.ConfigureAwait(false);
            var writer = new KafkaWriter(buffer);
            await using (writer.ConfigureAwait(false))
            {
                await payload
                    .WriteToAsync(writer, cancellationToken)
                    .ConfigureAwait(false);
            }

            var lengthBuffer = new MemoryStream();
            await using (lengthBuffer
                .ConfigureAwait(false))
            {
                var lengthWriter = new KafkaWriter(lengthBuffer);
                await using (lengthWriter.ConfigureAwait(false))
                {
                    await lengthWriter
                        .WriteInt32Async(Int32.From((int)buffer.Length), cancellationToken)
                        .ConfigureAwait(false);
                }

                await _networkClient.SendAsync(
                        lengthBuffer
                            .GetBuffer()
                            .AsMemory()
                            .Slice(0, (int)lengthBuffer.Length),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            
            await _networkClient.SendAsync(
                    buffer.GetBuffer().AsMemory()
                        .Slice(0, (int)buffer.Length),
                    cancellationToken)
                .ConfigureAwait(false);
        }

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