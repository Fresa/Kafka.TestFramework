using System.Threading;
using System.Threading.Tasks;

namespace Kafka.TestFramework
{
    internal class RequestClient : Client<RequestPayload>, IRequestClient
    {
        private RequestClient(INetworkClient networkClient) : base(networkClient)
        {
        }

        internal static RequestClient Start(INetworkClient networkClient)
        {
            var client = new RequestClient(networkClient);
            client.StartReceiving();
            return client;
        }

        public async ValueTask<ResponsePayload> ReadAsync(
            RequestPayload requestPayload,
            CancellationToken cancellationToken = default)
        {
            return await ResponsePayload
                .ReadFromAsync(
                    requestPayload,
                    Reader,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}