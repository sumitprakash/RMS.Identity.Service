namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class RoleTable
{
    public const string Name = "Role";

    public static class Columns
    {
        public const string RoleId = "RoleID";
        public const string RoleUuid = "RoleUUID";
        public const string Name = "Name";
        public const string Description = "Description";
        public const string IsSystemRole = "IsSystemRole";
        public const string IsDeleted = "IsDeleted";
        public const string CreatedAt = "CreatedAt";
    }
}
