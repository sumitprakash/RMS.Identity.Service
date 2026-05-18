namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class TenantSettingTable
{
    public const string Name = "TenantSetting";

    public static class Columns
    {
        public const string TenantSettingId = "TenantSettingID";
        public const string CompanyId = "CompanyID";
        public const string SettingKey = "SettingKey";
        public const string SettingValue = "SettingValue";
        public const string CreatedAt = "CreatedAt";
    }
}
