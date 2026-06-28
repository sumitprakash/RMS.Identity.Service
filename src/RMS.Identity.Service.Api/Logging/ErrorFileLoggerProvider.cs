using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace RMS.Identity.Service.Api.Logging;

public sealed class ErrorFileLoggerProvider : ILoggerProvider
{
    private readonly ErrorFileLoggerOptions _options;
    private readonly string _filePath;
    private readonly Channel<string> _entries;
    private readonly Task _writerTask;
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

    public bool IsEnabled(LogLevel logLevel) =>
        _options.Enabled &&
        logLevel != LogLevel.None &&
        logLevel >= _options.MinimumLevel;

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
}
