namespace RMS.Identity.Service.Infrastructure.Auth
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string SigningKeyEnvVar { get; set; } = "JWT_SIGNING_KEY";
        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 30;
    }
}
