using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

namespace RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateCompanyUserRequestBody
{
    [Required]
    [StringLength(32, MinimumLength = 8)]
    [RegularExpression("^[A-Za-z0-9]+$")]
    public string Username { get; init; } = string.Empty;

    [MinLength(2)]
    [MaxLength(64)]
    public string? DisplayName { get; init; }

    [Required]
    [EnumDataType(typeof(CompanyRole))]
    public CompanyRole? CompanyRole { get; init; }
}
