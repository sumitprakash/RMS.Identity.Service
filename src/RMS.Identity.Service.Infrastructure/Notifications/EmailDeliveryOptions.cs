namespace RMS.Identity.Service.Infrastructure.Notifications;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "EmailDelivery";

    public bool Enabled { get; init; }

    public string FromAddress { get; set; } = string.Empty;

    public string FromDisplayName { get; init; } = "RMS";

    public string VerificationUrlTemplate { get; init; } = string.Empty;

    public int PollIntervalSeconds { get; init; } = 30;

    public int BatchSize { get; init; } = 10;

    public int MaxRetries { get; init; } = 5;

    public int RetryDelaySeconds { get; init; } = 300;

    public int ProcessingTimeoutSeconds { get; init; } = 300;

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; } = 587;

    public bool EnableSsl { get; init; } = true;

    public string SmtpUsername { get; init; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string SmtpPasswordEnvVar { get; init; } = string.Empty;
}
