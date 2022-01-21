using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kafka.TestFramework
{
    internal class NetworkStream : Stream
    {
        private readonly INetworkClient _networkClient;

        public NetworkStream(INetworkClient networkClient)
        {
            _networkClient = networkClient;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken()) => 
            _networkClient.SendAsync(buffer, cancellationToken).AsValueTask();

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get; set; } = 0;
    }
}