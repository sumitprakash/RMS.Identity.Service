using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegisterCompanyRequestBody
{
    [Required(ErrorMessage = "Company legal name is required.")]
    [NotWhiteSpace(ErrorMessage = "Company legal name is required.")]
    [MaxLength(255, ErrorMessage = "Company legal name must not exceed 255 characters.")]
    public string LegalName { get; init; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Company trade name must not exceed 255 characters.")]
    public string? TradeName { get; init; }

    [Required(ErrorMessage = "GSTIN is required.")]
    [NotWhiteSpace(ErrorMessage = "GSTIN is required.")]
    [MaxLength(32, ErrorMessage = "GSTIN must not exceed 32 characters.")]
    [Gstin(ErrorMessage = "GSTIN must be a valid GSTIN.")]
    public string Gstin { get; init; } = string.Empty;

    [Required(ErrorMessage = "Company contact email address is required.")]
    [NotWhiteSpace(ErrorMessage = "Company contact email address is required.")]
    [EmailAddress(ErrorMessage = "Company contact email address must be a valid email address.")]
    [MaxLength(150, ErrorMessage = "Company contact email address must not exceed 150 characters.")]
    public string ContactEmailAddress { get; init; } = string.Empty;

    [Required(ErrorMessage = "Company contact phone number is required.")]
    [NotWhiteSpace(ErrorMessage = "Company contact phone number is required.")]
    [Phone(ErrorMessage = "Company contact phone number must be a valid phone number.")]
    [MaxLength(32, ErrorMessage = "Company contact phone number must not exceed 32 characters.")]
    public string ContactPhoneNumber { get; init; } = string.Empty;

    [Required(ErrorMessage = "Company registered address line 1 is required.")]
    [NotWhiteSpace(ErrorMessage = "Company registered address line 1 is required.")]
    [MaxLength(255, ErrorMessage = "Company registered address line 1 must not exceed 255 characters.")]
    public string AddressLine1 { get; init; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Company registered address line 2 must not exceed 255 characters.")]
    public string? AddressLine2 { get; init; }

    [Required(ErrorMessage = "Company city is required.")]
    [NotWhiteSpace(ErrorMessage = "Company city is required.")]
    [MaxLength(128, ErrorMessage = "Company city must not exceed 128 characters.")]
    public string City { get; init; } = string.Empty;

    [Required(ErrorMessage = "Company state is required.")]
    [NotWhiteSpace(ErrorMessage = "Company state is required.")]
    [MaxLength(128, ErrorMessage = "Company state must not exceed 128 characters.")]
    public string State { get; init; } = string.Empty;

    [Required(ErrorMessage = "Company postal code is required.")]
    [NotWhiteSpace(ErrorMessage = "Company postal code is required.")]
    [MaxLength(20, ErrorMessage = "Company postal code must not exceed 20 characters.")]
    public string PostalCode { get; init; } = string.Empty;

    [Required(ErrorMessage = "Company country is required.")]
    [NotWhiteSpace(ErrorMessage = "Company country is required.")]
    [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "Company country must be a two-letter country code.")]
    public string Country { get; init; } = "IN";
}
