using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RefreshRequestBody
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
