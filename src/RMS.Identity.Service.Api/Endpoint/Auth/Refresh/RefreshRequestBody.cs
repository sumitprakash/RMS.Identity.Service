using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RefreshRequestBody
{
    [Required]
    [NotEmptyOrWhiteSpace]
    [MinLength(64)]
    [MaxLength(256)]
    public required string RefreshToken { get; init; }
}
