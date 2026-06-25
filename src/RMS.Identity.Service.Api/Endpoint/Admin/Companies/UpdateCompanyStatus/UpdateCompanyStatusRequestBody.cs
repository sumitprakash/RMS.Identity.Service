using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Admin.Companies.UpdateCompanyStatus;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateCompanyStatusRequestBody
{
    [Required(ErrorMessage = "Company status is required.")]
    [NotWhiteSpace(ErrorMessage = "Company status is required.")]
    [NormalizedStringValues(
        "verified",
        "rejected",
        "suspended",
        ErrorMessage = "Company status must be verified, rejected, or suspended.")]
    public string Status { get; init; } = string.Empty;
}
