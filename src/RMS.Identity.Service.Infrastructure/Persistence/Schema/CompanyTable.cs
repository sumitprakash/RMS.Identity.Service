namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class CompanyTable
{
    public const string Name = "Company";

    public static class Columns
    {
        public const string CompanyId = "CompanyID";
        public const string CompanyUuid = "CompanyUUID";
        public const string CompanyCode = "CompanyCode";
        public const string CompanyName = "CompanyName";
        public const string CompanyGstin = "CompanyGSTIN";
        public const string IsDeleted = "IsDeleted";
        public const string CreatedAt = "CreatedAt";
        public const string CreatedBy = "CreatedBy";
        public const string UpdatedAt = "UpdatedAt";
        public const string UpdatedBy = "UpdatedBy";
    }
}
