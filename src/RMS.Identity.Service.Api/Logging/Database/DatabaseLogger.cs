using RMS.Identity.Service.Api.Logging.Common;

namespace RMS.Identity.Service.Api.Logging.Database;

public sealed class DatabaseLogger : ILogger
{
    private readonly string _categoryName;
    private readonly DatabaseLoggerProvider _provider;

    public DatabaseLogger(string categoryName, DatabaseLoggerProvider provider)
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

        _provider.Enqueue(new DatabaseLogEntry(
            DateTimeOffset.UtcNow,
            logLevel,
            _categoryName,
            eventId.Id,
            LogScopeFormatter.Capture(_provider.ScopeProvider),
            message,
            exception?.ToString()));
    }
}
