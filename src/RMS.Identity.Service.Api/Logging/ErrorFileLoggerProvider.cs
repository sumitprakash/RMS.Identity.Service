using Microsoft.Extensions.Logging;
using System.Collections;
using System.Text;
using System.Threading.Channels;

namespace RMS.Identity.Service.Api.Logging;

public sealed class ErrorFileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ErrorFileLoggerOptions _options;
    private readonly string _filePath;
    private readonly Channel<string> _entries;
    private readonly Task _writerTask;
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
    private int _disposed;

    public ErrorFileLoggerProvider(ErrorFileLoggerOptions options, IHostEnvironment environment)
    {
        _options = options;
        _filePath = System.IO.Path.IsPathFullyQualified(options.Path)
            ? options.Path
            : System.IO.Path.Combine(environment.ContentRootPath, options.Path);
        _entries = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _writerTask = Task.Run(ProcessQueueAsync);
    }

    public ILogger CreateLogger(string categoryName) => new ErrorFileLogger(categoryName, this);

    public IExternalScopeProvider ScopeProvider => _scopeProvider;

    public bool IsEnabled(LogLevel logLevel) =>
        _options.Enabled &&
        logLevel != LogLevel.None &&
        logLevel >= _options.MinimumLevel;

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public string GetScopeText()
    {
        var builder = new StringBuilder();
        _scopeProvider.ForEachScope(
            static (scope, builder) => AppendScope(scope, builder),
            builder);

        if (builder.Length == 0)
        {
            return " CorrelationTraceId=unavailable";
        }

        if (!builder.ToString().Contains("CorrelationTraceId=", StringComparison.Ordinal))
        {
            builder.Insert(0, "CorrelationTraceId=unavailable ");
        }

        return $" {builder}";
    }

    public void Enqueue(string entry)
    {
        if (Volatile.Read(ref _disposed) == 1)
        {
            return;
        }

        _entries.Writer.TryWrite(entry);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _entries.Writer.TryComplete();

        try
        {
            _writerTask.GetAwaiter().GetResult();
        }
        catch
        {
        }
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_filePath)!);

            await using var stream = new FileStream(
                _filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            await using var writer = new StreamWriter(stream);

            await foreach (var entry in _entries.Reader.ReadAllAsync())
            {
                await writer.WriteAsync(entry);
                await writer.FlushAsync();
            }
        }
        catch
        {
            // Logging must not break request processing.
        }
    }

    private static void AppendScope(object? scope, StringBuilder builder)
    {
        if (scope is IEnumerable<KeyValuePair<string, object?>> properties)
        {
            foreach (var property in properties)
            {
                if (string.Equals(property.Key, "{OriginalFormat}", StringComparison.Ordinal))
                {
                    continue;
                }

                AppendProperty(builder, property.Key, property.Value);
            }

            return;
        }

        if (scope is IEnumerable<KeyValuePair<string, string?>> stringProperties)
        {
            foreach (var property in stringProperties)
            {
                AppendProperty(builder, property.Key, property.Value);
            }

            return;
        }

        if (scope is not null)
        {
            AppendProperty(builder, "Scope", scope);
        }
    }

    private static void AppendProperty(StringBuilder builder, string key, object? value)
    {
        if (builder.Length > 0)
        {
            builder.Append(' ');
        }

        builder
            .Append(key)
            .Append('=')
            .Append(value?.ToString()?.ReplaceLineEndings(" ") ?? string.Empty);
    }
}
