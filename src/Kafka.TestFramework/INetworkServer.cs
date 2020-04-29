using System.Threading;
using System.Threading.Tasks;

namespace Kafka.TestFramework
{
    internal interface INetworkServer
    {
        Task<INetworkClient> WaitForConnectedClientAsync
            (CancellationToken cancellationToken = default);
    }
}