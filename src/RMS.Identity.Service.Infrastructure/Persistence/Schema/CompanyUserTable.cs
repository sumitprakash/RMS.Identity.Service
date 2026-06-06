namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class CompanyUserTable
{
    public const string Name = "CompanyUser";

    public static class Columns
    {
        public const string CompanyUserId = "CompanyUserID";
        public const string CompanyId = "CompanyID";
        public const string UserId = "UserID";
        public const string CompanyRole = "CompanyRole";
        public const string MembershipStatus = "MembershipStatus";
        public const string InvitedBy = "InvitedBy";
        public const string JoinedAt = "JoinedAt";
        public const string CreatedAt = "CreatedAt";
        public const string CreatedBy = "CreatedBy";
        public const string UpdatedAt = "UpdatedAt";
        public const string UpdatedBy = "UpdatedBy";
    }
}
