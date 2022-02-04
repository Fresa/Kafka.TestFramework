using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Log.It;

namespace Kafka.TestFramework
{
    internal abstract class Client : IAsyncDisposable
    {
        private readonly CancellationTokenSource _signalNoMoreDataToWrite;
        private readonly CancellationTokenSource _signalNoMoreDataToRead;

        private readonly Pipe _pipe = new Pipe(new PipeOptions(
            useSynchronizationContext: false));
        private readonly INetworkClient _networkClient;
        private Task _sendAndReceiveBackgroundTask = default!;
        private const int MinimumBufferSize = 512;
        private static readonly ILogger Logger =
            LogFactory.Create<Client>();

        protected Client(INetworkClient networkClient, CancellationToken cancellationToken)
        {
            _networkClient = networkClient;
            NetworkClient = new NetworkStream(networkClient);
            Reader = _pipe.Reader;
            _signalNoMoreDataToWrite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _signalNoMoreDataToRead = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pipe.Writer.OnReaderCompleted((exception, _) => _signalNoMoreDataToWrite.Cancel(), null);
            _pipe.Reader.OnWriterCompleted((exception, _) => _signalNoMoreDataToRead.Cancel(), null);
        }

        protected NetworkStream NetworkClient { get; }
        protected PipeReader Reader { get; }

        protected CancellationToken NoMoreDataToRead => _signalNoMoreDataToRead.Token;

        protected void StartReceiving()
        {
            _sendAndReceiveBackgroundTask = Task.Run(
                async () =>
                {
                    var cancellationToken = _signalNoMoreDataToWrite.Token;
                    var writer = _pipe.Writer;
                    await using (_networkClient.ConfigureAwait(false))
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
                                    break;
                                }

                                Logger.Debug("Received {bytesRead} bytes", bytesRead);
                                writer.Advance(bytesRead);

                                result = await writer
                                    .FlushAsync(cancellationToken)
                                    .ConfigureAwait(false);
                            } while (result.IsCanceled == false &&
                                     result.IsCompleted == false);
                        }
                        catch when (_signalNoMoreDataToWrite.IsCancellationRequested)
                        {
                            // Shutdown in progress
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            writer.Complete(ex);
                            throw;
                        }
                        finally
                        {
                            _signalNoMoreDataToWrite.Cancel();
                        }

                        writer.Complete();
                    }
                });
        }

        public async ValueTask DisposeAsync()
        {
            _signalNoMoreDataToWrite.Cancel();
            _signalNoMoreDataToRead.Cancel();

            await _sendAndReceiveBackgroundTask
                .ConfigureAwait(false);
        }
    }
}