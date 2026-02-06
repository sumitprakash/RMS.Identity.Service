namespace RMS.Identity.Service.Domain.Entities;

/// <summary>
/// Represents a tenant/company in RMS.
/// Identity service is the source of truth for Company.
/// </summary>
public class Company
{
    // Internal BIGINT Primary Key
    public long CompanyID { get; set; }

    // External UUID (API boundary)
    public Guid CompanyUUID { get; set; }

    public string CompanyCode { get; set; } = null!;

    public string CompanyName { get; set; } = null!;

    public string? CompanyGSTIN { get; set; }

    public bool IsDeleted { get; set; }

    // Audit columns
    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }
}