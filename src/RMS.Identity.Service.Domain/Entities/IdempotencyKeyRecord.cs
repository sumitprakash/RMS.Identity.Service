namespace RMS.Identity.Service.Domain.Entities;

public class IdempotencyKeyRecord
{
    public long IdempotencyKeyID { get; set; }

    public string KeyValue { get; set; } = null!;

    public string Method { get; set; } = null!;

    public string Route { get; set; } = null!;

    public string? RequestHash { get; set; }

    public int? ResponseCode { get; set; }

    public string? ResponseBody { get; set; }

    public DateTime CreatedAt { get; set; }
}
