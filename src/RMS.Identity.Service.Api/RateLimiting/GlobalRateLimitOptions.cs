namespace RMS.Identity.Service.Api.RateLimiting;

public sealed class GlobalRateLimitOptions
{
    public const string SectionName = "RateLimiting:Global";

    public bool Enabled { get; set; } = true;

    public int PermitLimit { get; set; } = 100;

    public int WindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; }

    public bool AutoReplenishment { get; set; } = true;

    public string RejectionMessage { get; set; } = "Too many requests. Try again later.";
}
