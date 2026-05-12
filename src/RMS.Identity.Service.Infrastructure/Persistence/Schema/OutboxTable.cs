namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class OutboxTable
{
    public const string Name = "Outbox";

    public static class Columns
    {
        public const string OutboxId = "OutboxID";
        public const string EventType = "EventType";
        public const string AggregateType = "AggregateType";
        public const string AggregateUuid = "AggregateUUID";
        public const string Payload = "Payload";
        public const string Status = "Status";
        public const string Retries = "Retries";
        public const string CreatedAt = "CreatedAt";
        public const string AvailableAt = "AvailableAt";
    }
}
