using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

namespace RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateCompanyUserRequestBody
{
    [Required]
    [MinLength(10)]
    [EmailAddress]
    [MaxLength(64)]
    public required string Username { get; init; }

    [MinLength(2)]
    [MaxLength(64)]
    public string? DisplayName { get; init; }

    [Required]
    [EnumDataType(typeof(CompanyRole))]
    public required CompanyRole CompanyRole { get; init; }
}
