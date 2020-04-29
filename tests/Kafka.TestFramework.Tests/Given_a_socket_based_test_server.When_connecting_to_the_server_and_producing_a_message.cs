using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Log.It;
using Xunit;
using Xunit.Abstractions;
using Record = Kafka.Protocol.Records.Record;

namespace Kafka.TestFramework.Tests
{
    public partial class Given_a_socket_based_test_server
    {
        public class When_connecting_to_the_server_and_producing_a_message : TestSpecificationAsync
        {
            private SocketBasedKafkaTestFramework _testServer;
            private IEnumerable<Record> _records;
           
            public When_connecting_to_the_server_and_producing_a_message(
                ITestOutputHelper testOutputHelper)
                : base(testOutputHelper)
            {
            }

            protected override Task GivenAsync()
            {
                _testServer = KafkaTestFramework.WithSocket();

                _testServer.On<ApiVersionsRequest, ApiVersionsResponse>(
                    request => request.Respond()
                        .WithAllApiKeys());

                _testServer.On<MetadataRequest, MetadataResponse>(
                    request => request.Respond()
                        .WithTopicsCollection(
                            request.TopicsCollection?.Select(topic =>
                                new Func<MetadataResponse.MetadataResponseTopic,
                                    MetadataResponse.MetadataResponseTopic>(
                                    responseTopic =>
                                        responseTopic
                                            .WithName(topic.Name)
                                            .WithPartitionsCollection(partition =>
                                                partition
                                                    .WithLeaderId(Int32.From(0))
                                                    .WithPartitionIndex(Int32.From(0))
                                                    .WithReplicaNodesCollection(new[] { Int32.From(0) }))))
                                .ToArray() ?? new Func<MetadataResponse.MetadataResponseTopic, MetadataResponse.MetadataResponseTopic>[0])
                        .WithControllerId(Int32.From(0))
                        .WithClusterId(String.From("test"))
                        .WithBrokersCollection(broker => broker
                            .WithRack(String.From("testrack"))
                            .WithNodeId(Int32.From(0))
                            .WithHost(String.From("localhost"))
                            .WithPort(Int32.From(_testServer.Port)))
                        );

                _testServer.On<ProduceRequest, ProduceResponse>(async request => (await request
                   .WithActionAsync(produceRequest => Task.Run(async () =>
                   {
                       _records = await produceRequest
                           .ExtractRecordsAsync(CancellationToken.None)
                           .ToListAsync()
                           .ConfigureAwait(false);
                   }))
                   .ConfigureAwait(false))
                   .Respond()
                   .WithResponsesCollection(request.TopicsCollection.Select(topicProduceData =>
                       new Func<ProduceResponse.TopicProduceResponse,
                           ProduceResponse.TopicProduceResponse>(
                           topicProduceResponse =>
                               topicProduceResponse
                                   .WithName(topicProduceData.Name)
                                   .WithPartitionsCollection(topicProduceData.PartitionsCollection.Select(partitionProduceData =>
                                       new Func<ProduceResponse.TopicProduceResponse.PartitionProduceResponse,
                                           ProduceResponse.TopicProduceResponse.PartitionProduceResponse>(
                                           partitionProduceResponse =>
                                               partitionProduceResponse
                                                   .WithPartitionIndex(partitionProduceData.PartitionIndex)
                                                   .WithLogAppendTimeMs(Int64.From(-1))))
                                       .ToArray())))
                       .ToArray()));

                return Task.CompletedTask;
            }

            protected override async Task WhenAsync()
            {
                await using (_testServer.Start()
                    .ConfigureAwait(false))
                {
                    await ProduceMessageFromClientAsync("localhost", _testServer.Port)
                        .ConfigureAwait(false);
                }
            }

            [Fact]
            public void It_should_have_read_one_record()
            {
                _records.Should().HaveCount(1);
            }

            [Fact]
            public void It_should_have_read_the_message_sent()
            {
                _records.First().Value.EncodeToString(Encoding.UTF8).Should().Be("test");
            }

            private static async Task ProduceMessageFromClientAsync(string host,
                int port)
            {
                var producerConfig = new ProducerConfig(new Dictionary<string, string>
                {
                    { "log_level", "7"}
                })
                {
                    BootstrapServers = $"{host}:{port}",
                    MessageTimeoutMs = 5000,
                    MetadataRequestTimeoutMs = 5000,
                    SocketTimeoutMs = 30000,
                    Debug = "all"
                };

                using var producer =
                    new ProducerBuilder<Null, string>(producerConfig)
                        .SetLogHandler(LogExtensions.UseLogIt)
                        .Build();

                var report = await producer
                    .ProduceAsync("my-topic",
                        new Message<Null, string> { Value = "test" })
                    .ConfigureAwait(false);
                LogFactory.Create("producer").Info("Produce report {@report}", report);

                producer.Flush();
            }
        }
    }
}