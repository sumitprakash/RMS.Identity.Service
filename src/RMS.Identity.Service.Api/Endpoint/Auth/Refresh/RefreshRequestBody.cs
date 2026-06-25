using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RefreshRequestBody
{
    [Required(ErrorMessage = "Refresh token is required.")]
    [NotWhiteSpace(ErrorMessage = "Refresh token is required.")]
    [MaxLength(256, ErrorMessage = "Refresh token must not exceed 256 characters.")]
    public string RefreshToken { get; init; } = string.Empty;
}
