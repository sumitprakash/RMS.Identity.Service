using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompany;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateCompanyRequestBody
{
    public string LegalName { get; init; } = string.Empty;

    public string? TradeName { get; init; }

    public string Gstin { get; init; } = string.Empty;

    public string ContactEmailAddress { get; init; } = string.Empty;

    public string ContactPhoneNumber { get; init; } = string.Empty;

    public string AddressLine1 { get; init; } = string.Empty;

    public string? AddressLine2 { get; init; }

    public string City { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;
}
