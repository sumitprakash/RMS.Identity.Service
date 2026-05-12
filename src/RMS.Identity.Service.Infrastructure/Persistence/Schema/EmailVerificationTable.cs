namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class EmailVerificationTable
{
    public const string Name = "EmailVerification";

    public static class Columns
    {
        public const string EmailVerificationId = "EmailVerificationID";
        public const string UserId = "UserID";
        public const string TokenHash = "TokenHash";
        public const string Purpose = "Purpose";
        public const string ExpiresAt = "ExpiresAt";
        public const string CreatedAt = "CreatedAt";
        public const string Consumed = "Consumed";
    }
}
