using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompany;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateCompanyRequestBody
{
    [Required]
    [MinLength(2)]
    [MaxLength(256)]
    public string LegalName { get; init; } = string.Empty;

    [MinLength(2)]
    [MaxLength(160)]
    public string? TradeName { get; init; }

    [Required]
    [StringLength(15, MinimumLength = 15)]
    [RegularExpression(@"(?i)^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$")]
    public string Gstin { get; init; } = string.Empty;

    [Required]
    [MinLength(10)]
    [EmailAddress]
    [MaxLength(64)]
    public string ContactEmailAddress { get; init; } = string.Empty;

    [Required]
    [StringLength(10, MinimumLength = 10)]
    [RegularExpression("^[0-9]{10}$")]
    public string ContactPhoneNumber { get; init; } = string.Empty;

    [Required]
    [MinLength(5)]
    [MaxLength(160)]
    public string AddressLine1 { get; init; } = string.Empty;

    [MinLength(2)]
    [MaxLength(256)]
    public string? AddressLine2 { get; init; }

    [Required]
    [MinLength(2)]
    [MaxLength(32)]
    public string City { get; init; } = string.Empty;

    [Required]
    [MinLength(2)]
    [MaxLength(32)]
    public string State { get; init; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression("^[0-9]{6}$")]
    public string PostalCode { get; init; } = string.Empty;

    [JsonIgnore]
    public string Country { get; init; } = "IN";
}
