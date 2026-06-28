namespace RMS.Identity.Service.Infrastructure.Maintenance;

public sealed class DataRetentionOptions
{
    public const string SectionName = "DataRetention";

    public bool Enabled { get; init; } = true;

    public int RunIntervalHours { get; init; } = 24;

    public int RefreshTokenRetentionDays { get; init; } = 30;

    public int VerificationTokenRetentionDays { get; init; } = 7;

    public int IdempotencyRetentionDays { get; init; } = 7;

    public int OutboxRetentionDays { get; init; } = 7;

    public int BatchSize { get; init; } = 1000;
}
