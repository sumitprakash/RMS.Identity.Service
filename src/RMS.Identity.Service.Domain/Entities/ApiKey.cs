namespace RMS.Identity.Service.Domain.Entities;

/// <summary>
/// API Keys for store integrations or machine-to-machine auth.
/// </summary>
public class ApiKey
{
    public long ApiKeyID { get; set; }

    public Guid ApiKeyUUID { get; set; }

    public long CompanyID { get; set; }

    public long? StoreID { get; set; }

    public string KeyHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}