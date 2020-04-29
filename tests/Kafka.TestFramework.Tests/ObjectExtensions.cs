using System;
using System.Threading.Tasks;

namespace Kafka.TestFramework.Tests
{
    internal static class ObjectExtensions
    {
        internal static async Task<T> WithActionAsync<T>(this T @object, Func<T, Task> invoke)
        {
            await invoke(@object);
            return @object;
        }
    }
}