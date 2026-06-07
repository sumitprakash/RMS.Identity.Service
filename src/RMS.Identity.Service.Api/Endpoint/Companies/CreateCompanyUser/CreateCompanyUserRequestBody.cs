using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateCompanyUserRequestBody
{
    public string Username { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string CompanyRole { get; init; } = string.Empty;
}
