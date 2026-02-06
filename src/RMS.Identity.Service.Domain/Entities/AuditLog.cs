namespace RMS.Identity.Service.Domain.Entities;

/// <summary>
/// Records identity-related actions (user created, login, role changes).
/// </summary>
public class AuditLog
{
    public long AuditID { get; set; }

    public string TableName { get; set; } = null!;

    public string RecordId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public long? ActorUserID { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }
}