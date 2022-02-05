using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Kafka.TestFramework
{
    internal sealed class SocketNetworkClient : INetworkClient
    {
        private readonly Socket _socket;

        public SocketNetworkClient(Socket socket)
        {
            _socket = socket;
        }

        public ValueTask DisposeAsync()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            _socket.Dispose();
            return new ValueTask();
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _socket
                    .SendAsync(buffer, SocketFlags.None, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (SocketException) when (!_socket.Connected)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _socket
                    .ReceiveAsync(buffer, SocketFlags.None, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (SocketException) when (!_socket.Connected)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}