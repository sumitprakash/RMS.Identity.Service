using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegisterCompanyRequestBody
{
    [Required]
    [NotBlank]
    [MinLength(2)]
    [MaxLength(255)]
    public required string LegalName { get; init; }

    [MinLength(2)]
    [MaxLength(160)]
    public string? TradeName { get; init; }

    [Required]
    [StringLength(15, MinimumLength = 15)]
    [RegularExpression(@"(?i)^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$")]
    public required string Gstin { get; init; }

    [Required]
    [MinLength(10)]
    [EmailAddress]
    [MaxLength(64)]
    public required string ContactEmailAddress { get; init; }

    [Required]
    [StringLength(10, MinimumLength = 10)]
    [RegularExpression("^[0-9]{10}$")]
    public required string ContactPhoneNumber { get; init; }

    [Required]
    [NotBlank]
    [MinLength(5)]
    [MaxLength(160)]
    public required string AddressLine1 { get; init; }

    [MinLength(2)]
    [MaxLength(255)]
    public string? AddressLine2 { get; init; }

    [Required]
    [NotBlank]
    [MinLength(2)]
    [MaxLength(32)]
    public required string City { get; init; }

    [Required]
    [NotBlank]
    [MinLength(2)]
    [MaxLength(32)]
    public required string State { get; init; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression("^[0-9]{6}$")]
    public required string PostalCode { get; init; }

    [JsonIgnore]
    public string Country { get; init; } = "IN";
}
