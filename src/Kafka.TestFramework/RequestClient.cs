using System.Threading;
using System.Threading.Tasks;
using Kafka.Protocol;

namespace Kafka.TestFramework
{
    internal class RequestClient : Client, IRequestClient
    {
        private RequestClient(INetworkClient networkClient, CancellationToken cancellationToken) : 
            base(networkClient, cancellationToken)
        {
        }

        internal static RequestClient Start(INetworkClient networkClient, CancellationToken cancellationToken)
        {
            var client = new RequestClient(networkClient, cancellationToken);
            client.StartReceiving();
            return client;
        }

        public ValueTask SendAsync(
            RequestPayload payload,
            CancellationToken cancellationToken = default)
        {
            return payload.WriteToAsync(NetworkClient, cancellationToken);
        }

        public async ValueTask<ResponsePayload> ReadAsync(
            RequestPayload requestPayload,
            CancellationToken cancellationToken = default)
        {
            return await ResponsePayload
                .ReadFromAsync(
                    requestPayload,
                    Reader,
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, NoMoreDataToRead).Token)
                .ConfigureAwait(false);
        }
    }
}