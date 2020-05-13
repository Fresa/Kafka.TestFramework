using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Kafka.Protocol;
using Kafka.Protocol.Records;
using Int16 = Kafka.Protocol.Int16;

namespace Kafka.TestFramework.Tests
{
    internal static class ProduceRequestExtensions
    {
        internal static async IAsyncEnumerable<RecordBatch> ExtractRecordBatchesAsync(
            this ProduceRequest produceRequest,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var records = produceRequest.TopicsCollection.SelectMany(data =>
                    data.PartitionsCollection.Select(produceData =>
                        produceData.Records))
                .Where(record => record.HasValue);

            var pipe = new Pipe();
            var reader = new KafkaReader(pipe.Reader);
            foreach (var record in records)
            {
                await pipe.Writer.WriteAsync(
                    record.Value.Value.AsMemory(),
                    cancellationToken);

                yield return await RecordBatch.ReadFromAsync(Int16.Default, reader,
                    cancellationToken);
            }
        }

        internal static async IAsyncEnumerable<Record> ExtractRecordsAsync(
            this ProduceRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var batch in request
                .ExtractRecordBatchesAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                if (batch.Records == null)
                    continue;

                foreach (var record in batch.Records)
                {
                    yield return record;
                }
            }
        }

    }
}