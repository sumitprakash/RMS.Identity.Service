namespace RMS.Identity.Service.Api.Logging.Database;

public sealed record DatabaseLogEntry(
    DateTimeOffset CreatedAt,
    LogLevel LogLevel,
    string Category,
    int EventId,
    IReadOnlyDictionary<string, string?> ScopeProperties,
    string Message,
    string? Exception);
