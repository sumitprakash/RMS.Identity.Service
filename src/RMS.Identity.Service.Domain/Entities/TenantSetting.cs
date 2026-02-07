namespace RMS.Identity.Service.Domain.Entities;

/// <summary>
/// Per-company configuration settings.
/// Example: invoice numbering rules, tax defaults.
/// </summary>
public class TenantSetting
{
    public long TenantSettingID { get; set; }

    public long CompanyID { get; set; }

    public string SettingKey { get; set; } = null!;

    public string SettingValueJson { get; set; } = "{}";

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }
}
