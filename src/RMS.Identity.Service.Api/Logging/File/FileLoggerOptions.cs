namespace RMS.Identity.Service.Api.Logging.File;

public sealed class FileLoggerOptions
{
    public const string SectionName = "FileLogging";

    public bool Enabled { get; set; } = true;

    public string Path { get; set; } = "logs/application.log";

    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}
