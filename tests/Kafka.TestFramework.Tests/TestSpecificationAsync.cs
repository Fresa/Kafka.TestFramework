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
        static TestSpecificationAsync()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            NLog.LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));
            LogFactory.Initialize(new NLogFactory(new LogicalThreadContext()));
        }

        public TestSpecificationAsync(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            NLogCapturingTarget.Subscribe += TestOutputHelper.WriteLine;
        }

        protected CancellationToken TimeoutCancellationToken =>
            new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;

        protected virtual Task TearDownAsync()
        {
            return Task.CompletedTask;
        }

        protected sealed override async Task DisposeAsync(bool disposing)
        {
            NLogCapturingTarget.Subscribe -= TestOutputHelper.WriteLine;
            await TearDownAsync();
            await base.DisposeAsync(disposing);
        }
    }
}