using RMS.Identity.Service.Api.Logging.Database;
using RMS.Identity.Service.Api.Logging.File;

namespace RMS.Identity.Service.Api.Logging;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddFileLogger(
        this ILoggingBuilder builder,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration
            .GetSection(FileLoggerOptions.SectionName)
            .Get<FileLoggerOptions>() ?? new FileLoggerOptions();

        builder.AddProvider(new FileLoggerProvider(options, environment));
        return builder;
    }

    public static ILoggingBuilder AddDatabaseLogger(
        this ILoggingBuilder builder,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(DatabaseLoggerOptions.SectionName)
            .Get<DatabaseLoggerOptions>() ?? new DatabaseLoggerOptions();
        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);

        builder.AddProvider(new DatabaseLoggerProvider(options, connectionString));
        return builder;
    }
}
