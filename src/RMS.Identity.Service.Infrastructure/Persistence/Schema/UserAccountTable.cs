namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class UserAccountTable
{
    public const string Name = "UserAccount";

    public static class Columns
    {
        public const string UserId = "UserID";
        public const string UserUuid = "UserUUID";
        public const string CompanyId = "CompanyID";
        public const string Username = "Username";
        public const string PasswordHash = "PasswordHash";
        public const string DisplayName = "DisplayName";
        public const string PhoneNumber = "PhoneNumber";
        public const string PasswordSetupRequired = "PasswordSetupRequired";
        public const string EmailVerified = "EmailVerified";
        public const string IsActive = "IsActive";
        public const string IsDeleted = "IsDeleted";
        public const string LastLoginAt = "LastLoginAt";
        public const string FailedLoginCount = "FailedLoginCount";
        public const string LockedUntil = "LockedUntil";
        public const string CreatedAt = "CreatedAt";
        public const string CreatedBy = "CreatedBy";
        public const string UpdatedAt = "UpdatedAt";
        public const string UpdatedBy = "UpdatedBy";
    }
}
