using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SignUpRequestBody
{
    [Required]
    [EmailAddress]
    public string EmailAddress { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string FirstName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    [Required]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; init; } = string.Empty;
}
