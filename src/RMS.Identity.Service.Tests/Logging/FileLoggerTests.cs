using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Api.Logging.File;

namespace RMS.Identity.Service.Tests.Logging;

public sealed class FileLoggerTests
{
    [Fact]
    public void Log_WithScope_WritesScopeValuesToFile()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"rms-identity-logs-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDirectory);
            using var provider = new FileLoggerProvider(
                new FileLoggerOptions
                {
                    Path = "application.log",
                    MinimumLevel = LogLevel.Information
                },
                new TestHostEnvironment(tempDirectory));
            var logger = provider.CreateLogger("TestCategory");

            using (logger.BeginScope(new Dictionary<string, object?>
                   {
                       ["CorrelationTraceId"] = "trace-test-1"
                   }))
            {
                logger.LogInformation("Something happened.");
            }

            provider.Dispose();

            var log = File.ReadAllText(Path.Combine(tempDirectory, "application.log"));
            Assert.Contains("CorrelationTraceId=trace-test-1", log);
            Assert.Contains("Something happened.", log);
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
            using var provider = new FileLoggerProvider(
                new FileLoggerOptions
                {
                    Path = "application.log",
                    MinimumLevel = LogLevel.Information
                },
                new TestHostEnvironment(tempDirectory));
            var logger = provider.CreateLogger("TestCategory");

            logger.LogInformation("Something happened.");

            provider.Dispose();

            var log = File.ReadAllText(Path.Combine(tempDirectory, "application.log"));
            Assert.Contains("CorrelationTraceId=unavailable", log);
            Assert.Contains("Something happened.", log);
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
