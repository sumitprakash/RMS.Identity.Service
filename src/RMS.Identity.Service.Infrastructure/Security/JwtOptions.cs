namespace RMS.Identity.Service.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "RMS.Identity.Service";

    public string Audience { get; set; } = "RMS";

    public string SigningKey { get; set; } = string.Empty;

    public string SigningKeyEnvVar { get; set; } = string.Empty;

    public int AccessTokenLifetimeSeconds { get; set; } = 3600;

    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
