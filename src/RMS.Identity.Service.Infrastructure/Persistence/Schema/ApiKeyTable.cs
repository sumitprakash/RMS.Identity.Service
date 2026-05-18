namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class ApiKeyTable
{
    public const string Name = "ApiKey";

    public static class Columns
    {
        public const string ApiKeyId = "ApiKeyID";
        public const string ApiKeyUuid = "ApiKeyUUID";
        public const string CompanyId = "CompanyID";
        public const string StoreId = "StoreID";
        public const string KeyHash = "KeyHash";
        public const string Description = "Description";
        public const string IsActive = "IsActive";
        public const string CreatedAt = "CreatedAt";
        public const string CreatedBy = "CreatedBy";
    }
}
