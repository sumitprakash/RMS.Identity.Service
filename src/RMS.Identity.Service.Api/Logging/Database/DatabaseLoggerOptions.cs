namespace RMS.Identity.Service.Api.Logging.Database;

public sealed class DatabaseLoggerOptions
{
    public const string SectionName = "DatabaseLogging";

    public bool Enabled { get; set; } = true;

    public string ConnectionStringName { get; set; } = "Default";

    public string TableName { get; set; } = "ApplicationLog";

    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    public int CommandTimeoutSeconds { get; set; } = 5;
}
