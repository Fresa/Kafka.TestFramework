using System;
using System.Threading;
using System.Threading.Tasks;
using Kafka.Protocol;

namespace Kafka.TestFramework
{
    public interface IRequestClient : IAsyncDisposable
    {
        ValueTask<ResponsePayload> ReadAsync(
            RequestPayload requestPayload,
            CancellationToken cancellationToken = default);

        ValueTask SendAsync(
            RequestPayload payload,
            CancellationToken cancellationToken = default);
    }
}