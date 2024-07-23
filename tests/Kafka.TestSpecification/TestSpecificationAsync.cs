using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Log.It;
using Log.It.With.NLog;
using Microsoft.Extensions.Configuration;
using NLog.Extensions.Logging;
using Test.It.With.XUnit;
using Xunit.Abstractions;

namespace Kafka.TestFramework
{
    public abstract class TestSpecificationAsync : XUnit2SpecificationAsync
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IDisposable _logWriter;

        static TestSpecificationAsync()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            NLog.LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));
            LogFactory.Initialize(new NLogFactory(new LogicalThreadContext()));
            NLogCapturingTarget.Subscribe += Output.Writer.WriteLine;
        }

        public TestSpecificationAsync(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _logWriter = Output.WriteTo(testOutputHelper);
        }

        public void WriteKafkaLogMessage(
            string name,
            string facility,
            string level,
            string message)
        {
            _testOutputHelper?.WriteLine(
                $"{DateTimeOffset.Now:HH:mm:ss.fff} | {name}:{facility} | {level} | {message}");
        }

        protected CancellationToken TimeoutCancellationToken =>
            new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;

        protected sealed override async Task DisposeAsync(bool disposing)
        {
            await base.DisposeAsync(disposing);
            _logWriter.Dispose();
        }
    }
}