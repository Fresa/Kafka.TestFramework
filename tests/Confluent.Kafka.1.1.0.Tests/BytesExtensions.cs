using System.Text;
using Kafka.Protocol;

namespace Kafka.TestFramework.Tests
{
    internal static class BytesExtensions
    {
        internal static string EncodeToString(this byte[] bytes, Encoding encoding)
        {
            return encoding.GetString(bytes);
        }
    }
}