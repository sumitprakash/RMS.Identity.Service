using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateCompanyUserRequestBody
{
    [Required(ErrorMessage = "Username is required.")]
    [NotWhiteSpace(ErrorMessage = "Username is required.")]
    [EmailAddress(ErrorMessage = "Username must be a valid email address.")]
    [MaxLength(150, ErrorMessage = "Username must not exceed 150 characters.")]
    public string Username { get; init; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Display name must not exceed 255 characters.")]
    public string? DisplayName { get; init; }

    [Required(ErrorMessage = "Company role is required.")]
    [NotWhiteSpace(ErrorMessage = "Company role is required.")]
    [NormalizedStringValues(
        "OWNER",
        "ADMIN",
        "MEMBER",
        ErrorMessage = "Company role must be OWNER, ADMIN, or MEMBER.")]
    public string CompanyRole { get; init; } = string.Empty;
}
