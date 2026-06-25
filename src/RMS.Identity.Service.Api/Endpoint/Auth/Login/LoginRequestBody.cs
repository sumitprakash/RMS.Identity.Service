using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LoginRequestBody
{
    [Required(ErrorMessage = "Username is required.")]
    [NotWhiteSpace(ErrorMessage = "Username is required.")]
    [EmailAddress(ErrorMessage = "Username must be a valid email address.")]
    [MaxLength(150, ErrorMessage = "Username must not exceed 150 characters.")]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [NotWhiteSpace(ErrorMessage = "Password is required.")]
    [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters.")]
    public string Password { get; init; } = string.Empty;
}
