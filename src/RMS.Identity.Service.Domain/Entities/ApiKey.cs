namespace RMS.Identity.Service.Domain.Entities;

public class ApiKey
{
    public long ApiKeyID { get; set; }

    public Guid ApiKeyUUID { get; set; }

    public long CompanyID { get; set; }

    public long? StoreID { get; set; }

    public string KeyHash { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }
}
