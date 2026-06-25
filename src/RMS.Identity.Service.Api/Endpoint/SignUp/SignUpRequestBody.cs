using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SignUpRequestBody
{
    [Required(ErrorMessage = "Email address is required.")]
    [NotWhiteSpace(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Email address must be a valid email address.")]
    [MaxLength(150, ErrorMessage = "Email address must not exceed 150 characters.")]
    public string EmailAddress { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [NotWhiteSpace(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [NotWhiteSpace(ErrorMessage = "First name is required.")]
    [MaxLength(100, ErrorMessage = "First name must not exceed 100 characters.")]
    public string FirstName { get; init; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Middle name must not exceed 100 characters.")]
    public string? MiddleName { get; init; }

    [Required(ErrorMessage = "Last name is required.")]
    [NotWhiteSpace(ErrorMessage = "Last name is required.")]
    [MaxLength(100, ErrorMessage = "Last name must not exceed 100 characters.")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [NotWhiteSpace(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Phone number must be a valid phone number.")]
    [MaxLength(32, ErrorMessage = "Phone number must not exceed 32 characters.")]
    public string PhoneNumber { get; init; } = string.Empty;
}
