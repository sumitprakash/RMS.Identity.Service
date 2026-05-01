using System.Data.Common;

namespace RMS.Identity.Service.Infrastructure.Persistence;

internal static class MySqlDataReaderExtensions
{
    public static string? GetNullableString(this DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static DateTime GetUtcDateTime(this DbDataReader reader, string columnName)
    {
        var value = reader.GetDateTime(reader.GetOrdinal(columnName));
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
