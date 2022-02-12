using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Kafka.Protocol;

namespace Kafka.TestFramework
{
    public abstract class KafkaTestFramework
    {
        private readonly INetworkServer _networkServer;

        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();

        private readonly List<Task> _backgroundTasks = new List<Task>();

        private const int HasStopped = 0;
        private const int HasStarted = 1;
        private int _status = HasStopped;

        /// <summary>
        /// Triggered when the test framework is stopping
        /// </summary>
        public CancellationToken Stopping => _cancellationTokenSource.Token;

        public static InMemoryKafkaTestFramework InMemory()
        {
            return new InMemoryKafkaTestFramework(
                new BufferBlock<INetworkClient>());
        }

        public static SocketBasedKafkaTestFramework WithSocket()
        {
            var server = SocketServer.Start();
            return new SocketBasedKafkaTestFramework(server);
        }

        public static SocketBasedKafkaTestFramework WithSocket(
            IPAddress localIpAddress,
            int port = 0)
        {
            var server = SocketServer.Start(localIpAddress, port);
            return new SocketBasedKafkaTestFramework(server);
        }

        internal KafkaTestFramework(INetworkServer networkServer)
        {
            _networkServer = networkServer;
        }

        public IAsyncDisposable Start()
        {
            var previousStatus = Interlocked.Exchange(ref _status, HasStarted);
            if (previousStatus == HasStarted)
            {
                return new StopOnDispose(this);
            }

            var task = Task.Run(
                async () =>
                {
                    while (_cancellationTokenSource.IsCancellationRequested == false)
                    {
                        try
                        {
                            var client = await _networkServer
                                .WaitForConnectedClientAsync(Stopping)
                                .ConfigureAwait(false);
                            ReceiveMessagesFor(client);
                        }
                        catch when (_cancellationTokenSource.IsCancellationRequested)
                        {
                            return;
                        }
                        catch
                        {
                            _cancellationTokenSource.Cancel();
                            throw;
                        }
                    }
                });
            _backgroundTasks.Add(task);
            return new StopOnDispose(this);
        }

        private void ReceiveMessagesFor(INetworkClient networkClient)
        {
            var task = Task.Run(
                async () =>
                {
                    var client = ResponseClient.Start(networkClient, Stopping);
                    await using var _ = client.ConfigureAwait(false);
                    while (_cancellationTokenSource.IsCancellationRequested == false)
                    {
                        try
                        {
                            var requestPayload = await client
                                .ReadAsync(Stopping)
                                .ConfigureAwait(false);

                            if (!_subscriptions.TryGetValue(
                                requestPayload.Message.GetType(),
                                out var subscription))
                            {
                                throw new InvalidOperationException(
                                    $"Missing subscription for {requestPayload.Message.GetType()}");
                            }

                            var response = await subscription(
                                requestPayload.Message,
                                Stopping);

                            await client
                                .SendAsync(
                                    new ResponsePayload(
                                        new ResponseHeader(
                                                Messages.GetResponseHeaderVersionFor(requestPayload))
                                            .WithCorrelationId(requestPayload.Header.CorrelationId),
                                        response),
                                    Stopping)
                                .ConfigureAwait(false);
                        }
                        catch when (_cancellationTokenSource.IsCancellationRequested)
                        {
                            return;
                        }
                        catch (OperationCanceledException)
                        {
                            _cancellationTokenSource.Cancel();
                            return;
                        }
                        catch
                        {
                            _cancellationTokenSource.Cancel();
                            throw;
                        }
                    }
                });
            _backgroundTasks.Add(task);
        }

        private readonly Dictionary<Type, MessageSubscription> _subscriptions =
            new Dictionary<Type, MessageSubscription>();

        private delegate Task<Message> MessageSubscription(
            Message message,
            CancellationToken cancellationToken = default);

        public KafkaTestFramework On<TRequestMessage, TResponseMessage>(
            Func<TRequestMessage, TResponseMessage> subscription)
            where TRequestMessage : Message, IRespond<TResponseMessage>
            where TResponseMessage : Message
        {
            _subscriptions.Add(
                typeof(TRequestMessage),
                (message, cancellationToken) => Task.Run<Message>(
                    () => subscription.Invoke((TRequestMessage)message), cancellationToken));
            return this;
        }

        public KafkaTestFramework On<TRequestMessage, TResponseMessage>(
            Func<TRequestMessage, Task<TResponseMessage>> subscription)
            where TRequestMessage : Message, IRespond<TResponseMessage>
            where TResponseMessage : Message
        {
            _subscriptions.Add(
                typeof(TRequestMessage),
                async (message, _) => await subscription.Invoke((TRequestMessage)message));
            return this;
        }

        public KafkaTestFramework On<TRequestMessage, TResponseMessage>(
            Func<TRequestMessage, CancellationToken, Task<TResponseMessage>> subscription)
            where TRequestMessage : Message, IRespond<TResponseMessage>
            where TResponseMessage : Message
        {
            _subscriptions.Add(
                typeof(TRequestMessage),
                async (message, cancellationToken) =>
                    await subscription.Invoke((TRequestMessage)message, cancellationToken));
            return this;
        }

        internal sealed class StopOnDispose : IAsyncDisposable
        {
            private readonly KafkaTestFramework _testFramework;

            public StopOnDispose(KafkaTestFramework testFramework)
            {
                _testFramework = testFramework;
            }

            public ValueTask DisposeAsync()
            {
                _testFramework._cancellationTokenSource.Cancel();
                return Task.WhenAll(_testFramework._backgroundTasks)
                    .ThrowAllExceptions()
                    .AsValueTask();
            }
        }
    }
}