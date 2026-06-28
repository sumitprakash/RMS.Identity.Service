using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Api.Logging;

namespace RMS.Identity.Service.Tests.Logging;

public sealed class ErrorFileLoggerTests
{
    [Fact]
    public void Log_WithScope_WritesScopeValuesToFile()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rms-identity-logs-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDirectory);
            using var provider = new ErrorFileLoggerProvider(
                new ErrorFileLoggerOptions
                {
                    Path = "errors.log",
                    MinimumLevel = LogLevel.Error
                },
                new TestHostEnvironment(tempDirectory));
            var logger = provider.CreateLogger("TestCategory");

            using (logger.BeginScope(new Dictionary<string, object?>
                   {
                       ["CorrelationTraceId"] = "trace-test-1",
                       ["RequestTraceId"] = "request-test-1"
                   }))
            {
                logger.LogError(new InvalidOperationException("failure"), "Something failed.");
            }

            provider.Dispose();

            var log = File.ReadAllText(Path.Combine(tempDirectory, "errors.log"));
            Assert.Contains("CorrelationTraceId=trace-test-1", log);
            Assert.Contains("RequestTraceId=request-test-1", log);
            Assert.Contains("Something failed.", log);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Log_WithoutScope_WritesUnavailableCorrelationTrace()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rms-identity-logs-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDirectory);
            using var provider = new ErrorFileLoggerProvider(
                new ErrorFileLoggerOptions
                {
                    Path = "errors.log",
                    MinimumLevel = LogLevel.Error
                },
                new TestHostEnvironment(tempDirectory));
            var logger = provider.CreateLogger("TestCategory");

            logger.LogError("Something failed.");

            provider.Dispose();

            var log = File.ReadAllText(Path.Combine(tempDirectory, "errors.log"));
            Assert.Contains("CorrelationTraceId=unavailable", log);
            Assert.Contains("Something failed.", log);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }


    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "RMS.Identity.Service.Tests";

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
