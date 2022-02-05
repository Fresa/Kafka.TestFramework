using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Kafka.Protocol;
using Kafka.Protocol.Records;
using Log.It;
using Xunit;
using Xunit.Abstractions;
using Int32 = Kafka.Protocol.Int32;
using Record = Kafka.Protocol.Records.Record;

namespace Kafka.TestFramework.Tests
{
    public partial class Given_a_socket_based_test_server
    {
        public class When_connecting_to_the_server_and_consuming_a_message : TestSpecificationAsync
        {
            private SocketBasedKafkaTestFramework _testServer;
            private ConsumeResult<Ignore, string> _result;

            public When_connecting_to_the_server_and_consuming_a_message(
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
                                                        .WithLeaderId(0)
                                                        .WithPartitionIndex(0))))
                                .ToArray() ??
                            Array.Empty<Func<MetadataResponse.MetadataResponseTopic,
                                MetadataResponse.MetadataResponseTopic>>())
                        .WithBrokersCollection(broker => broker
                            .WithHost("localhost")
                            .WithPort(_testServer.Port))
                );

                _testServer.On<FindCoordinatorRequest, FindCoordinatorResponse>(
                    request => request.Respond()
                        .WithHost("localhost")
                        .WithPort(_testServer.Port)
                );

                _testServer.On<JoinGroupRequest, JoinGroupResponse>(
                    request => request.Respond()
                        .WithProtocolName(request.ProtocolsCollection.First().Value.Name));

                _testServer.On<SyncGroupRequest, SyncGroupResponse>(
                    async (request, cancellationToken) => request.Respond()
                        .WithAssignment(
                            await new ConsumerProtocolAssignment(ConsumerProtocolAssignment.MaxVersion)
                                .WithAssignedPartitionsCollection(partition => partition
                                    .WithTopic("topic1")
                                    .WithPartitionsCollection(new Int32[] { 0 }))
                                .ToBytesAsync(cancellationToken)
                                .ConfigureAwait(false))
                );

                _testServer.On<OffsetFetchRequest, OffsetFetchResponse>(request => request.Respond()
                    .WithTopicsCollection(
                        request.TopicsCollection?.Select(topic =>
                                new Func<OffsetFetchResponse.OffsetFetchResponseTopic,
                                    OffsetFetchResponse.OffsetFetchResponseTopic>(responseTopic =>
                                    responseTopic
                                        .WithName(topic.Name)
                                        .WithPartitionsCollection(topic.PartitionIndexesCollection
                                            .Select(partitionIndex =>
                                                new Func<OffsetFetchResponse.OffsetFetchResponseTopic.
                                                    OffsetFetchResponsePartition,
                                                    OffsetFetchResponse.OffsetFetchResponseTopic.
                                                    OffsetFetchResponsePartition>(
                                                    partition => partition
                                                        .WithPartitionIndex(partitionIndex)))
                                            .ToArray())))
                            .ToArray() ??
                        Array.Empty<Func<OffsetFetchResponse.OffsetFetchResponseTopic,
                            OffsetFetchResponse.OffsetFetchResponseTopic>>()));

                _testServer.On<FetchRequest, FetchResponse>(request => request.Respond()
                    .WithResponsesCollection(
                        request.TopicsCollection.Select(topic =>
                            new Func<FetchResponse.FetchableTopicResponse, FetchResponse.FetchableTopicResponse>(
                                response => response
                                    .WithTopic(topic.Topic)
                                    .WithPartitionsCollection(topic.PartitionsCollection.Select(partition =>
                                            new Func<FetchResponse.FetchableTopicResponse.PartitionData,
                                                FetchResponse.FetchableTopicResponse.PartitionData>(data => data
                                                .WithRecords(new NullableRecordBatch
                                                {
                                                    Magic = 2,
                                                    Records = new NullableArray<Record>(new Record
                                                    {
                                                        Value = Encoding.UTF8.GetBytes(
                                                            "data fetched from broker"),
                                                    })
                                                }))).ToArray()
                                    ))).ToArray())
                );

                _testServer.On<OffsetCommitRequest, OffsetCommitResponse>(request => request.Respond()
                    .WithTopicsCollection(request.TopicsCollection.Select(topic =>
                            new Func<OffsetCommitResponse.OffsetCommitResponseTopic,
                                OffsetCommitResponse.OffsetCommitResponseTopic>(responseTopic => responseTopic
                                .WithName(topic.Name)
                                .WithPartitionsCollection(topic.PartitionsCollection.Select(partition =>
                                    new Func<OffsetCommitResponse.OffsetCommitResponseTopic.
                                        OffsetCommitResponsePartition,
                                        OffsetCommitResponse.OffsetCommitResponseTopic.
                                        OffsetCommitResponsePartition>(responsePartition => responsePartition
                                        .WithPartitionIndex(partition.PartitionIndex))).ToArray())
                            )).ToArray()
                    ));

                _testServer.On<LeaveGroupRequest, LeaveGroupResponse>(request => request.Respond());
                _testServer.On<HeartbeatRequest, HeartbeatResponse>(request => request.Respond());

                return Task.CompletedTask;
            }

            protected override async Task WhenAsync()
            {
                await using (_testServer.Start()
                    .ConfigureAwait(false))
                {
                    _result = ConsumeMessage("localhost", _testServer.Port, _testServer.Stopping);
                }
            }

            [Fact]
            public void It_should_have_read_the_message_sent()
            {
                _result.Message.Value.Should().Be("data fetched from broker");
            }

            private ConsumeResult<Ignore, string> ConsumeMessage(string host,
                int port, CancellationToken testServerStopping)
            {
                var consumerConfig = new ConsumerConfig(new Dictionary<string, string>
                {
                    { "log_level", "7" }
                })
                {
                    BootstrapServers = $"{host}:{port}",
                    ApiVersionRequestTimeoutMs = 30000,
                    Debug = "all",
                    GroupId = "group1"
                };

                using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig)
                    .SetLogHandler(LogExtensions.UseLogIt)
                    .Build();

                consumer.Subscribe("topic1");
                var cancellationToken = CancellationTokenSource
                    .CreateLinkedTokenSource(testServerStopping, TimeoutCancellationToken).Token;
                var result = consumer.Consume(cancellationToken);
                consumer.Close();

                LogFactory.Create("consumer").Info("Consumer consumed {@result}", result);

                return result;
            }
        }
    }
}