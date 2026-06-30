using System.Threading.Channels;
using MySqlConnector;

namespace RMS.Identity.Service.Api.Logging.Database;

public sealed class DatabaseLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly DatabaseLoggerOptions _options;
    private readonly string? _connectionString;
    private readonly string _tableName;
    private readonly Channel<DatabaseLogEntry> _entries;
    private readonly Task _writerTask;
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
    private int _disposed;

    public DatabaseLoggerProvider(DatabaseLoggerOptions options, string? connectionString)
    {
        _options = options;
        _connectionString = connectionString;
        _tableName = IsValidIdentifier(options.TableName) ? options.TableName : "ApplicationLog";
        _entries = Channel.CreateUnbounded<DatabaseLogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _writerTask = Task.Run(ProcessQueueAsync);
    }

    public IExternalScopeProvider ScopeProvider => _scopeProvider;

    public ILogger CreateLogger(string categoryName) => new DatabaseLogger(categoryName, this);

    public bool IsEnabled(LogLevel logLevel) =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_connectionString) &&
        logLevel != LogLevel.None &&
        logLevel >= _options.MinimumLevel;

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public void Enqueue(DatabaseLogEntry entry)
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
            _writerTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
        }
    }

    private async Task ProcessQueueAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return;
        }

        await foreach (var entry in _entries.Reader.ReadAllAsync())
        {
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandTimeout = Math.Max(1, _options.CommandTimeoutSeconds);
                command.CommandText =
                    $"""
                    INSERT INTO `{_tableName}`
                        (CreatedAt, LogLevel, Category, EventID, CorrelationTraceID, Message, Exception)
                    VALUES
                        (@CreatedAt, @LogLevel, @Category, @EventID, @CorrelationTraceID, @Message, @Exception);
                    """;
                command.Parameters.AddWithValue("@CreatedAt", entry.CreatedAt.UtcDateTime);
                command.Parameters.AddWithValue("@LogLevel", entry.LogLevel.ToString());
                command.Parameters.AddWithValue("@Category", entry.Category);
                command.Parameters.AddWithValue("@EventID", entry.EventId);
                command.Parameters.AddWithValue("@CorrelationTraceID", GetScopeValue(entry, "CorrelationTraceId"));
                command.Parameters.AddWithValue("@Message", entry.Message);
                command.Parameters.AddWithValue("@Exception", (object?)entry.Exception ?? DBNull.Value);
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // Logging must not break request processing.
            }
        }
    }

    private static object GetScopeValue(DatabaseLogEntry entry, string key) =>
        entry.ScopeProperties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : DBNull.Value;

    private static bool IsValidIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 64
        && value.All(static character => char.IsAsciiLetterOrDigit(character) || character == '_');
}
