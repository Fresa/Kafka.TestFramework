using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kafka.TestFramework
{
    internal sealed class CrossWiredMemoryNetworkClient : INetworkClient
    {
        private readonly INetworkClient _first;
        private readonly INetworkClient _second;

        public CrossWiredMemoryNetworkClient(INetworkClient first, INetworkClient second)
        {
            _first = first;
            _second = second;
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask();
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return await _first
                .ReceiveAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return await _second
                .SendAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}