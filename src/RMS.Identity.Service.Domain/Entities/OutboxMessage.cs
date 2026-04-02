namespace RMS.Identity.Service.Domain.Entities;

public class OutboxMessage
{
    public long OutboxID { get; set; }

    public string EventType { get; set; } = null!;

    public string? AggregateType { get; set; }

    public Guid? AggregateUUID { get; set; }

    public string PayloadJson { get; set; } = null!;

    public string Status { get; set; } = "pending";

    public int Retries { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime AvailableAt { get; set; }
}
