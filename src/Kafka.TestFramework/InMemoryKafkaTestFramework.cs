using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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
            return RequestClient.Start(requestClient);
        }
    }
}