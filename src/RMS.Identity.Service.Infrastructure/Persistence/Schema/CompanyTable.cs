namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class CompanyTable
{
    public const string Name = "Company";

    public static class Columns
    {
        public const string CompanyId = "CompanyID";
        public const string CompanyUuid = "CompanyUUID";
        public const string CompanyCode = "CompanyCode";
        public const string LegalName = "LegalName";
        public const string TradeName = "TradeName";
        public const string CompanyGstin = "CompanyGSTIN";
        public const string ContactEmailAddress = "ContactEmailAddress";
        public const string ContactPhoneNumber = "ContactPhoneNumber";
        public const string AddressLine1 = "AddressLine1";
        public const string AddressLine2 = "AddressLine2";
        public const string City = "City";
        public const string State = "State";
        public const string PostalCode = "PostalCode";
        public const string Country = "Country";
        public const string CompanyStatus = "CompanyStatus";
        public const string IsDeleted = "IsDeleted";
        public const string CreatedAt = "CreatedAt";
        public const string CreatedBy = "CreatedBy";
        public const string UpdatedAt = "UpdatedAt";
        public const string UpdatedBy = "UpdatedBy";
    }
}
