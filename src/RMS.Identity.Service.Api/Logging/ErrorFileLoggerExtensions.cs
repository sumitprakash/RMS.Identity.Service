using Microsoft.Extensions.Logging;

namespace RMS.Identity.Service.Api.Logging;

public static class ErrorFileLoggerExtensions
{
    public static ILoggingBuilder AddErrorFileLogger(
        this ILoggingBuilder builder,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration
            .GetSection(ErrorFileLoggerOptions.SectionName)
            .Get<ErrorFileLoggerOptions>() ?? new ErrorFileLoggerOptions();

        builder.AddProvider(new ErrorFileLoggerProvider(options, environment));
        return builder;
    }
}
