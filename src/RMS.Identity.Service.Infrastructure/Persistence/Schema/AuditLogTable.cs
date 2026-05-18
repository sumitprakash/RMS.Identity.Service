namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class AuditLogTable
{
    public const string Name = "AuditLog";

    public static class Columns
    {
        public const string AuditId = "AuditID";
        public const string TableName = "TableName";
        public const string RecordId = "RecordId";
        public const string Action = "Action";
        public const string ActorUserId = "ActorUserID";
        public const string Payload = "Payload";
        public const string CreatedAt = "CreatedAt";
    }
}
