using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompanyUser;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateCompanyUserRequestBody
{
    [Required]
    [EnumDataType(typeof(CompanyRole))]
    public CompanyRole? CompanyRole { get; init; }

    [Required]
    [EnumDataType(typeof(CompanyMembershipStatus))]
    public CompanyMembershipStatus? MembershipStatus { get; init; }
}
