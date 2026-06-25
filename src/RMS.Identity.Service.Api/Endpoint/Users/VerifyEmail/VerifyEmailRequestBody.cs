using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Users.VerifyEmail;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class VerifyEmailRequestBody
{
    [Required(ErrorMessage = "Verification token is required.")]
    [NotWhiteSpace(ErrorMessage = "Verification token is required.")]
    [MaxLength(256, ErrorMessage = "Verification token must not exceed 256 characters.")]
    public string Token { get; init; } = string.Empty;

    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters.")]
    public string? Password { get; init; }
}
