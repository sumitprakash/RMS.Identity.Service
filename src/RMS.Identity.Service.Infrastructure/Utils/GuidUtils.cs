namespace RMS.Identity.Service.Infrastructure.Utils
{
    public static class GuidUtils
    {
        public static byte[] ToBytes(Guid g) => g == Guid.Empty ? new byte[16] : g.ToByteArray();

        public static Guid FromBytes(object dbValue)
        {
            if (dbValue == DBNull.Value || dbValue == null)
                return Guid.Empty;

            if (dbValue is byte[] bytes && bytes.Length == 16)
                return new Guid(bytes);

            if (dbValue is ReadOnlyMemory<byte> rom && rom.Length == 16)
                return new Guid(rom.ToArray());

            throw new InvalidCastException(
                $"Cannot convert value of type {dbValue.GetType()} to Guid");
        }

    }
}
