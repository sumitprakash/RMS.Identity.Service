using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.ModelBinding;

namespace RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;

[ModelBinder(BinderType = typeof(ApiRequestModelBinder<RegisterCompanyRequest>))]
public sealed class RegisterCompanyRequest
{
    [FromBody]
    [Required]
    public RegisterCompanyRequestBody Body { get; set; } = default!;
}
