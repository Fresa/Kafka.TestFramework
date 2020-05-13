using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Log.It;

namespace Kafka.TestFramework
{
    internal class DataReceiver
    {
        private readonly INetworkClient _networkClient;
        private const int MinimumBufferSize = 512;

        private static readonly ILogger Logger =
            LogFactory.Create<DataReceiver>();

        internal DataReceiver(INetworkClient networkClient)
        {
            _networkClient = networkClient;
        }

        internal async Task ReceiveAsync(PipeWriter writer, CancellationToken cancellationToken)
        {
            try
            {
                FlushResult result;
                do
                {
                    var memory = writer.GetMemory(MinimumBufferSize);
                    var bytesRead = await _networkClient.ReceiveAsync(
                            memory,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        return;
                    }

                    Logger.Debug("Received {bytesRead} bytes", bytesRead);
                    writer.Advance(bytesRead);

                    result = await writer
                        .FlushAsync(cancellationToken)
                        .ConfigureAwait(false);
                } while (result.IsCanceled == false &&
                         result.IsCompleted == false);
            }
            catch (Exception ex)
            {
                writer.Complete(ex);
                throw;
            }

            writer.Complete();
        }
    }
}