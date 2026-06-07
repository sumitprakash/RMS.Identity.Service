using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegisterCompanyRequestBody
{
    [Required]
    public string LegalName { get; init; } = string.Empty;

    public string? TradeName { get; init; }

    [Required]
    public string Gstin { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string ContactEmailAddress { get; init; } = string.Empty;

    [Required]
    [Phone]
    public string ContactPhoneNumber { get; init; } = string.Empty;

    [Required]
    public string AddressLine1 { get; init; } = string.Empty;

    public string? AddressLine2 { get; init; }

    [Required]
    public string City { get; init; } = string.Empty;

    [Required]
    public string State { get; init; } = string.Empty;

    [Required]
    public string PostalCode { get; init; } = string.Empty;

    [Required]
    public string Country { get; init; } = "IN";
}
