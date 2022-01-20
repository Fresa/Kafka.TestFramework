using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Kafka.Protocol;

namespace Kafka.TestFramework
{
    public sealed class InMemoryKafkaTestFramework : KafkaTestFramework
    {
        private readonly ITargetBlock<INetworkClient> _clients;

        internal InMemoryKafkaTestFramework(
            BufferBlock<INetworkClient> clients) 
            : base(InMemoryServer.Start(clients))
        {
            _clients = clients;
        }

        public async Task<IRequestClient> CreateRequestClientAsync(
            CancellationToken cancellationToken = default)
        {
            var first = new InMemoryNetworkClient();
            var second = new InMemoryNetworkClient();
            var requestClient = new CrossWiredMemoryNetworkClient(first, second);
            var responseClient = new CrossWiredMemoryNetworkClient(second, first);
            await _clients
                .SendAsync(responseClient, cancellationToken)
                .ConfigureAwait(false);
            return new DisposableRequestClientDecorator(RequestClient.Start(requestClient), responseClient, second, first);
        }

        private class DisposableRequestClientDecorator : IRequestClient
        {
            private readonly IRequestClient _requestClient;
            private readonly List<IAsyncDisposable> _disposables;

            public DisposableRequestClientDecorator(IRequestClient requestClient, params IAsyncDisposable[] disposables)
            {
                _requestClient = requestClient;
                _disposables = new List<IAsyncDisposable>(disposables) { requestClient };
            }
            public async ValueTask DisposeAsync()
            {
                await _disposables.DisposeAllAsync()
                    .ConfigureAwait(false);
            }

            public ValueTask<ResponsePayload> ReadAsync(RequestPayload requestPayload, CancellationToken cancellationToken = default)
            {
                return _requestClient.ReadAsync(requestPayload, cancellationToken);
            }

            public ValueTask SendAsync(RequestPayload payload, CancellationToken cancellationToken = default)
            {
                return _requestClient.SendAsync(payload, cancellationToken);
            }
        }
    }
}