using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Kafka.TestFramework
{
    internal static class TaskExtensions
    {
        internal static async Task ThrowAllExceptions(this Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                if (task.Exception?.InnerExceptions.Count > 1)
                {
                    ExceptionDispatchInfo.Capture(task.Exception).Throw();
                }

                throw;
            }
        }

        internal static ValueTask AsValueTask(this Task task) =>
            task.IsCompletedSuccessfully ? default : new ValueTask(task);
    }
}