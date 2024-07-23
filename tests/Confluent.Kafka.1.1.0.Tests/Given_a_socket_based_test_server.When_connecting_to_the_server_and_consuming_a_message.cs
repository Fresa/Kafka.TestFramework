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
            private readonly List<string> _result = new List<string>();
            private const int NumberOfMessage = 5;

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

                var records = new Dictionary<long, Record>();
                for (var i = 0; i < NumberOfMessage; i++)
                {
                    records.Add(i, new Record
                    {
                        OffsetDelta = i,
                        Value = Encoding.UTF8.GetBytes(
                            $"data{i} fetched from broker")
                    });
                }

                _testServer.On<FetchRequest, FetchResponse>(async (request, cancellationToken) =>
                {
                    var returnsData = false;
                    var response = request.Respond()
                        .WithResponsesCollection(
                            request.TopicsCollection.Select(topic =>
                                new Func<FetchResponse.FetchableTopicResponse, FetchResponse.FetchableTopicResponse>(
                                    response => response
                                        .WithTopic(topic.Topic)
                                        .WithPartitionsCollection(topic.PartitionsCollection.Select(partition =>
                                                new Func<FetchResponse.FetchableTopicResponse.PartitionData,
                                                    FetchResponse.FetchableTopicResponse.PartitionData>(data =>
                                                {
                                                    var recordBatch = new NullableRecordBatch
                                                    {
                                                        LastOffsetDelta = (int)partition.FetchOffset,
                                                        Magic = 2,
                                                        Records = records.TryGetValue(partition.FetchOffset.Value,
                                                            out var record)
                                                            ? new NullableArray<Record>(record)
                                                            : NullableArray<Record>.Default
                                                    };
                                                    returnsData = recordBatch.Records != NullableArray<Record>.Default;
                                                    return returnsData ? data.WithRecords(recordBatch) : data;
                                                })).ToArray()
                                        ))).ToArray());
                    if (!returnsData)
                    {
                        await Task.Delay(request.MaxWaitMs, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    return response;
                });

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
                    ConsumeMessages("localhost", _testServer.Port, _testServer.Stopping);
                }
            }

            [Fact]
            public void It_should_have_read_the_messages_sent()
            {
                _result.Should().HaveCount(NumberOfMessage);
                for (var i = 0; i < NumberOfMessage; i++)
                {
                    _result.Should().Contain($"data{i} fetched from broker");
                }
            }

            private void ConsumeMessages(string host,
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
                    GroupId = "group1",
                };

                using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig)
                    .SetLogHandler(this.Log)
                    .Build();

                consumer.Subscribe("topic1");
                var cancellationToken = CancellationTokenSource
                    .CreateLinkedTokenSource(testServerStopping, TimeoutCancellationToken).Token;
                try
                {
                    for (var i = 0; i < NumberOfMessage; i++)
                    {
                        _result.Add(consumer.Consume(cancellationToken).Message.Value);
                    }
                }
                finally
                {
                    consumer.Close();
                }
            }
        }
    }
}