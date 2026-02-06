using System.Data;

namespace RMS.Identity.Service.Infrastructure.Utils
{
    public static class ReaderExtensions
    {
        public static string? GetStringOrNull(this IDataRecord r, string name)
            => r.IsDBNull(r.GetOrdinal(name)) ? null : r.GetString(r.GetOrdinal(name));

        public static Guid GetGuid(this IDataRecord r, string name)
            => GuidUtils.FromBytes(r[name]);

        public static long? GetNullableLong(this IDataRecord r, string name)
            => r.IsDBNull(r.GetOrdinal(name)) ? null : (long?)r.GetInt64(r.GetOrdinal(name));
    }
}
