namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class RefreshTokenTable
{
    public const string Name = "RefreshToken";

    public static class Columns
    {
        public const string RefreshTokenId = "RefreshTokenID";
        public const string UserId = "UserID";
        public const string TokenHash = "TokenHash";
        public const string ExpiresAt = "ExpiresAt";
        public const string CreatedAt = "CreatedAt";
        public const string RevokedAt = "RevokedAt";
        public const string ReplacedByTokenHash = "ReplacedByTokenHash";
    }
}
