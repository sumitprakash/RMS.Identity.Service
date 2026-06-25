using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompanyUser;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateCompanyUserRequestBody
{
    [Required(ErrorMessage = "Company role is required.")]
    [NotWhiteSpace(ErrorMessage = "Company role is required.")]
    [NormalizedStringValues(
        "OWNER",
        "ADMIN",
        "MEMBER",
        ErrorMessage = "Company role must be OWNER, ADMIN, or MEMBER.")]
    public string CompanyRole { get; init; } = string.Empty;

    [Required(ErrorMessage = "Membership status is required.")]
    [NotWhiteSpace(ErrorMessage = "Membership status is required.")]
    [NormalizedStringValues(
        "active",
        "invited",
        "suspended",
        ErrorMessage = "Membership status must be active, invited, or suspended.")]
    public string MembershipStatus { get; init; } = string.Empty;
}
