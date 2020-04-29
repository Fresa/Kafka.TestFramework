using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kafka.TestFramework
{
    internal sealed class InMemoryNetworkClient : INetworkClient
    {
        private readonly Pipe _pipe = new Pipe();

        public ValueTask DisposeAsync()
        {
            return new ValueTask();
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _pipe.Writer
                .WriteAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
            return buffer.Length;
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var result = await _pipe.Reader
                .ReadAsync(cancellationToken)
                .ConfigureAwait(false);

            var length = result.Buffer.Length > buffer.Length ?
                buffer.Length :
                (int)result.Buffer.Length;

            result
                .Buffer
                .Slice(0, length)
                .CopyTo(
                    buffer
                        .Slice(0, length)
                        .Span);

            var position = result.Buffer.GetPosition(length);
            _pipe.Reader.AdvanceTo(position);
            return length;
        }
    }
}