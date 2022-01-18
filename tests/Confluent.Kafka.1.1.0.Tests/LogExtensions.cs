using System;
using Confluent.Kafka;
using Log.It;

namespace Kafka.TestFramework.Tests
{
    internal static class LogExtensions
    {
        internal static void UseLogIt<TKey, TValue>(
            IProducer<TKey, TValue> producer, LogMessage logMessage)
        {
            var logger = LogFactory.Create(producer.GetType().GetPrettyName());

            switch (logMessage.Level)
            {
                case SyslogLevel.Debug:
                    LogTo(logger.Debug);
                    break;
                case SyslogLevel.Notice:
                case SyslogLevel.Info:
                    LogTo(logger.Info);
                    break;
                case SyslogLevel.Warning:
                    LogTo(logger.Warning);
                    break;
                case SyslogLevel.Error:
                    LogTo(logger.Error);
                    break;
                case SyslogLevel.Alert:
                case SyslogLevel.Critical:
                case SyslogLevel.Emergency:
                    LogTo(logger.Fatal);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"{logMessage.Level} is not supported");
            }

            void LogTo(Action<string, object[]> log)
            {
                log("{name} {facility}: {message}",
                    new object[]
                    {
                        logMessage.Name,
                        logMessage.Facility,
                        logMessage.Message
                    });
            }
        }
    }
}