using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RMS.Identity.Service.Application.Identity.Internal;

internal static class RequestHashing
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string Compute(object value)
    {
        var json = JsonSerializer.Serialize(value, SerializerOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }
}
