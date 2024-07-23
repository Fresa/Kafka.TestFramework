using System.Text;

namespace Kafka.TestFramework
{
    public static class BytesExtensions
    {
        public static string EncodeToString(this byte[] bytes, Encoding encoding)
        {
            return encoding.GetString(bytes);
        }
    }
}