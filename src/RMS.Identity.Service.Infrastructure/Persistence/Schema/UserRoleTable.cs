namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class UserRoleTable
{
    public const string Name = "UserRole";

    public static class Columns
    {
        public const string UserRoleId = "UserRoleID";
        public const string UserId = "UserID";
        public const string RoleId = "RoleID";
        public const string AssignedAt = "AssignedAt";
        public const string AssignedBy = "AssignedBy";
    }
}
