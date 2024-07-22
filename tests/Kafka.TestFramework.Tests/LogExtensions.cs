using System;
using Confluent.Kafka;
using Log.It;

namespace Kafka.TestFramework.Tests
{
    internal static class LogExtensions
    {
        internal static void Log<TKey, TValue>(this TestSpecificationAsync test,
            IProducer<TKey, TValue> producer, LogMessage logMessage)
        {
            test.Log(logMessage);
        }

        internal static void Log<TKey, TValue>(this TestSpecificationAsync test,
            IConsumer<TKey, TValue> consumer, LogMessage logMessage)
        {
            test.Log(logMessage);
        }

        private static void Log(this TestSpecificationAsync test,
            LogMessage logMessage)
        {
            test.WriteKafkaLogMessage(logMessage.Name,
                logMessage.Facility,
                Enum.GetName(logMessage.Level),
                logMessage.Message);
        }
    }
}