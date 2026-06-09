namespace RMS.Identity.Service.Domain.Entities.Outbox;

public sealed record OutboxMessage(
    long OutboxId,
    string EventType,
    string Payload,
    int Retries);
