﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kafka.TestFramework
{
    internal static class ValueTaskExtensions
    {
        internal static Task WhenAllAsync(
            this IEnumerable<ValueTask> tasks)
            => Task.WhenAll(
                tasks.Where(
                        valueTask
                            => !valueTask.IsCompletedSuccessfully)
                    .Select(valueTask => valueTask.AsTask()));
    }
}