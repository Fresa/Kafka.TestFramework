﻿using System.Threading;
using System.Threading.Tasks;
using Kafka.Protocol;

namespace Kafka.TestFramework
{
    internal class ResponseClient : Client
    {
        private ResponseClient(INetworkClient networkClient) : base(networkClient)
        {
        }

        internal static ResponseClient Start(INetworkClient networkClient)
        {
            var client = new ResponseClient(networkClient);
            client.StartReceiving();
            return client;
        }

        internal ValueTask SendAsync(
            ResponsePayload payload,
            CancellationToken cancellationToken = default)
        {
            return payload.WriteToAsync(NetworkClient, cancellationToken);
        }

        internal async Task<RequestPayload> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            return await RequestPayload
                .ReadFromAsync(
                    RequestHeader.MaxVersion,
                    Reader,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}