using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompanyUser;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateCompanyUserRequestBody
{
    public string CompanyRole { get; init; } = string.Empty;

    public string MembershipStatus { get; init; } = string.Empty;
}
