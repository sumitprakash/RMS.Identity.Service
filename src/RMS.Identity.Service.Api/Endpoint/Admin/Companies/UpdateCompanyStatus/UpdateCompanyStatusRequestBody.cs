using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.Admin.Companies.UpdateCompanyStatus;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateCompanyStatusRequestBody
{
    public string Status { get; init; } = string.Empty;
}
