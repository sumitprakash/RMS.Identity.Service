using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.ModelBinding;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;

[ModelBinder(BinderType = typeof(ApiRequestModelBinder<RegisterCompanyRequest>))]
public sealed class RegisterCompanyRequest : IValidatableRequest
{
    [FromBody]
    [Required]
    public RegisterCompanyRequestBody Body { get; set; } = default!;
}
