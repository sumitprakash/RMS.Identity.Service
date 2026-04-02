namespace RMS.Identity.Service.Domain.Entities;

public class TenantSetting
{
    public long TenantSettingID { get; set; }

    public long CompanyID { get; set; }

    public string SettingKey { get; set; } = null!;

    public string? SettingValueJson { get; set; }

    public DateTime CreatedAt { get; set; }
}
