﻿using System.Threading;
using System.Threading.Tasks;
using Kafka.Protocol;

namespace Kafka.TestFramework
{
    internal class ResponseClient : Client
    {
        private ResponseClient(INetworkClient networkClient, CancellationToken cancellationToken) : 
            base(networkClient, cancellationToken)
        {
        }

        internal static ResponseClient Start(INetworkClient networkClient, CancellationToken cancellationToken)
        {
            var client = new ResponseClient(networkClient, cancellationToken);
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
                    Reader,
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, NoMoreDataToRead).Token)
                .ConfigureAwait(false);
        }
    }
}