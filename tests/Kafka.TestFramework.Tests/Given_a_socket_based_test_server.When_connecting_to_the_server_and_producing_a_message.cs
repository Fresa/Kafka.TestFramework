using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Kafka.Protocol;
using Log.It;
using Xunit;
using Xunit.Abstractions;
using Int32 = Kafka.Protocol.Int32;
using Int64 = Kafka.Protocol.Int64;
using Record = Kafka.Protocol.Records.Record;
using String = Kafka.Protocol.String;

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
                                .ToArray() ?? Array.Empty<Func<MetadataResponse.MetadataResponseTopic, MetadataResponse.MetadataResponseTopic>>())
                        .WithControllerId(Int32.From(0))
                        .WithClusterId(String.From("test"))
                        .WithBrokersCollection(broker => broker
                            .WithRack(String.From("testrack"))
                            .WithNodeId(Int32.From(0))
                            .WithHost(String.From("localhost"))
                            .WithPort(Int32.From(_testServer.Port)))
                        );
                
                _testServer.On<ProduceRequest, ProduceResponse>(request =>
                {
                    _records = request.TopicDataCollection.SelectMany(pair =>
                        pair.Value.PartitionDataCollection.Where(data => data.Records != null)
                            .SelectMany(data => data.Records!.Records));
                    return request
                        .Respond()
                        .WithResponsesCollection(request.TopicDataCollection.Select(topicProduceData =>
                                new Func<ProduceResponse.TopicProduceResponse,
                                    ProduceResponse.TopicProduceResponse>(
                                    topicProduceResponse =>
                                        topicProduceResponse
                                            .WithName(topicProduceData.Value.Name)
                                            .WithPartitionResponsesCollection(topicProduceData.Value
                                                .PartitionDataCollection.Select(partitionProduceData =>
                                                    new Func<ProduceResponse.TopicProduceResponse.
                                                        PartitionProduceResponse,
                                                        ProduceResponse.TopicProduceResponse.PartitionProduceResponse>(
                                                        partitionProduceResponse =>
                                                            partitionProduceResponse
                                                                .WithIndex(partitionProduceData.Index)
                                                                .WithLogAppendTimeMs(Int64.From(-1))))
                                                .ToArray())))
                            .ToArray());
                });

                return Task.CompletedTask;
            }

            protected override async Task WhenAsync()
            {
                await using (_testServer.Start()
                    .ConfigureAwait(false))
                {
                    await ProduceMessageFromClientAsync("localhost", _testServer.Port, _testServer.Stopping)
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
                int port, CancellationToken testServerStopping)
            {
                var producerConfig = new ProducerConfig(new Dictionary<string, string>
                {
                    { "log_level", "7"}
                })
                {
                    BootstrapServers = $"{host}:{port}",
                    ApiVersionRequestTimeoutMs = 30000,
                    Debug = "all"
                };

                using var producer =
                    new ProducerBuilder<Null, string>(producerConfig)
                        .SetLogHandler(LogExtensions.UseLogIt)
                        .Build();

                var report = await producer
                    .ProduceAsync("my-topic",
                        new Message<Null, string> { Value = "test" }, testServerStopping)
                    .ConfigureAwait(false);
                LogFactory.Create("producer").Info("Produce report {@report}", report);

                producer.Flush(testServerStopping);
            }
        }
    }
}