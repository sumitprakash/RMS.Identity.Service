using System.Globalization;
using RMS.Identity.Service.Api.Logging.Common;

namespace RMS.Identity.Service.Api.Logging.File;

public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProvider _provider;

    public FileLogger(string categoryName, FileLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        _provider.ScopeProvider.Push(state);

    public bool IsEnabled(LogLevel logLevel) => _provider.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrWhiteSpace(message) && exception is null)
        {
            return;
        }

        var scopeProperties = LogScopeFormatter.Capture(_provider.ScopeProvider);
        _provider.Enqueue(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{DateTimeOffset.UtcNow:O} [{logLevel}] {_categoryName} [{eventId.Id}]{LogScopeFormatter.Format(scopeProperties)} {message}{Environment.NewLine}{exception}{Environment.NewLine}"));
    }
}
