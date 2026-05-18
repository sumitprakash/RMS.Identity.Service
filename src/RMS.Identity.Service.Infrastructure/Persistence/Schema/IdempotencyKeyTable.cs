namespace RMS.Identity.Service.Infrastructure.Persistence.Schema;

internal static class IdempotencyKeyTable
{
    public const string Name = "IdempotencyKey";

    public static class Columns
    {
        public const string IdempotencyKeyId = "IdempotencyKeyID";
        public const string KeyValue = "KeyValue";
        public const string Method = "Method";
        public const string Route = "Route";
        public const string RequestHash = "RequestHash";
        public const string ResponseCode = "ResponseCode";
        public const string ResponseBody = "ResponseBody";
        public const string CreatedAt = "CreatedAt";
    }
}
