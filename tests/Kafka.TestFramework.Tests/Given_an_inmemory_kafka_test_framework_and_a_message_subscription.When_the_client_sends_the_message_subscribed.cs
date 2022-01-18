using System.Threading.Tasks;
using FluentAssertions;
using Kafka.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Kafka.TestFramework.Tests
{
    public partial class Given_an_inmemory_kafka_test_framework_and_a_message_subscription
    {
        public partial class
            When_the_client_sends_the_message_subscribed :
                TestSpecificationAsync
        {
            private readonly InMemoryKafkaTestFramework _testServer =
                KafkaTestFramework.InMemory();

            private ResponsePayload _response;

            public When_the_client_sends_the_message_subscribed(
                ITestOutputHelper testOutputHelper)
                : base(testOutputHelper)
            {
            }

            protected override Task GivenAsync()
            {
                _testServer.On<ApiVersionsRequest, ApiVersionsResponse>(
                    request => request.Respond()
                        .WithThrottleTimeMs(Int32.From(100))
                        .WithApiKeysCollection(
                            key => key
                                .WithApiKey(FetchRequest.ApiKey)
                                .WithMinVersion(FetchRequest.MinVersion)
                                .WithMaxVersion(FetchRequest.MaxVersion)));

                return Task.CompletedTask;
            }

            protected override async Task WhenAsync()
            {
                await using (_testServer.Start()
                    .ConfigureAwait(false))
                {
                    var client = await _testServer
                        .CreateRequestClientAsync()
                        .ConfigureAwait(false);

                    var message = new ApiVersionsRequest(ApiVersionsRequest.MaxVersion);
                    var requestPayload = new RequestPayload(
                        new RequestHeader(message.HeaderVersion)
                            .WithRequestApiKey(ApiVersionsRequest.ApiKey)
                            .WithRequestApiVersion(
                                message.Version)
                            .WithCorrelationId(Int32.From(12)),
                        message
                    );

                    await client
                        .SendAsync(requestPayload)
                        .ConfigureAwait(false);

                    _response = await client
                        .ReadAsync(requestPayload)
                        .ConfigureAwait(false);
                }
            }

            [Fact]
            public void
                The_subscription_should_receive_a_api_versions_response()
            {
                _response.Message.Should().BeOfType<ApiVersionsResponse>();
            }

            [Fact]
            public void
                The_subscription_should_receive_a_api_versions_response_with_correlation_id()
            {
                _response.Header.CorrelationId.Should().Be(Int32.From(12));
            }

            [Fact]
            public void
                The_subscription_should_receive_a_api_versions_response_with_throttle_time()
            {
                _response.Message.As<ApiVersionsResponse>()
                    .ThrottleTimeMs.Should().Be(Int32.From(100));
            }

            [Fact]
            public void
                The_subscription_should_receive_a_api_versions_response_with_one_api_key()
            {
                _response.Message.As<ApiVersionsResponse>()
                    .ApiKeysCollection.Should().HaveCount(1);
            }

            [Fact]
            public void
                The_subscription_should_receive_a_api_versions_response_with_fetch_request_api_key()
            {
                _response.Message.As<ApiVersionsResponse>()
                    .ApiKeysCollection.Value.Should().ContainKey(FetchRequest.ApiKey);
            }

            [Fact]
            public void
                The_subscription_should_receive_a_api_versions_response_with_fetch_request_api_index()
            {
                _response.Message.As<ApiVersionsResponse>()
                    .ApiKeysCollection[FetchRequest.ApiKey].ApiKey.Should()
                    .Be(FetchRequest.ApiKey);
            }

            [Fact]
            public void
                The_subscription_should_receive_a_api_versions_response_with_fetch_request_api_min_version()
            {
                _response.Message.As<ApiVersionsResponse>()
                    .ApiKeysCollection[FetchRequest.ApiKey].MinVersion.Should()
                    .Be(FetchRequest.MinVersion);
            }

            [Fact]
            public void
                The_subscription_should_receive_a_api_versions_response_with_fetch_request_api_max_version()
            {
                _response.Message.As<ApiVersionsResponse>()
                    .ApiKeysCollection[FetchRequest.ApiKey].MaxVersion.Should()
                    .Be(FetchRequest.MaxVersion);
            }

            protected override async Task TearDownAsync()
            {
                await _testServer
                    .DisposeAsync()
                    .ConfigureAwait(false);
            }
        }
    }
}