using System.Globalization;
using Microsoft.Extensions.Logging;

namespace RMS.Identity.Service.Api.Logging;

public sealed class ErrorFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ErrorFileLoggerProvider _provider;

    public ErrorFileLogger(string categoryName, ErrorFileLoggerProvider provider)
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

        _provider.Enqueue(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{DateTimeOffset.UtcNow:O} [{logLevel}] {_categoryName} [{eventId.Id}]{_provider.GetScopeText()} {message}{Environment.NewLine}{exception}{Environment.NewLine}"));
    }
}
