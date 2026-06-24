using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.ModelBinding;

namespace RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;

[ModelBinder(BinderType = typeof(ApiRequestModelBinder<CreateCompanyUserRequest>))]
public sealed class CreateCompanyUserRequest
{
    [FromRoute(Name = "companyUuid")]
    public Guid CompanyUuid { get; set; }

    [FromBody]
    [Required]
    public CreateCompanyUserRequestBody Body { get; set; } = default!;
}
