namespace RMS.Identity.Service.Infrastructure.Security;

internal sealed class JwtOptions
{
    public string Issuer { get; set; } = "rms.identity";

    public string Audience { get; set; } = "rms.api";

    public string? SigningKey { get; set; }

    public string? SigningKeyEnvVar { get; set; }

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 30;
}
