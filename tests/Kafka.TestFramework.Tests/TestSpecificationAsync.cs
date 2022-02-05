using System;
using System.Threading;
using System.Threading.Tasks;
using Log.It;
using Log.It.With.NLog;
using Microsoft.Extensions.Configuration;
using NLog.Extensions.Logging;
using Test.It.With.XUnit;
using Xunit.Abstractions;

namespace Kafka.TestFramework.Tests
{
    public class TestSpecificationAsync : XUnit2SpecificationAsync
    {
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
            _logWriter = Output.WriteTo(testOutputHelper);
        }

        protected CancellationToken TimeoutCancellationToken =>
            new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;

        protected sealed override async Task DisposeAsync(bool disposing)
        {
            await base.DisposeAsync(disposing);
            _logWriter.Dispose();
        }
    }
}