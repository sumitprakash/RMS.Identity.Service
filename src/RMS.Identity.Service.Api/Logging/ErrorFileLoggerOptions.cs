namespace RMS.Identity.Service.Api.Logging;

public sealed class ErrorFileLoggerOptions
{
    public const string SectionName = "FileLogging";

    public bool Enabled { get; set; } = true;

    public string Path { get; set; } = "logs/errors.log";

    public LogLevel MinimumLevel { get; set; } = LogLevel.Error;
}
