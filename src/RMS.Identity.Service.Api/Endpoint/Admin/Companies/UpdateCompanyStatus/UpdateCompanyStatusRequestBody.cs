using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RMS.Identity.Service.Domain.Contracts.Companies;

namespace RMS.Identity.Service.Api.Endpoint.Admin.Companies.UpdateCompanyStatus;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateCompanyStatusRequestBody
{
    [Required]
    [EnumDataType(typeof(CompanyStatusUpdate))]
    public required CompanyStatusUpdate Status { get; init; }
}
