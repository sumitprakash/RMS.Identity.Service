using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RMS.Identity.Service.Api.Endpoint.Companies;

[ModelBinder(BinderType = typeof(RegisterCompanyRequestModelBinder))]
public sealed class RegisterCompanyRequest
{
    public RegisterCompanyRequest(RegisterCompanyRequestBody body)
    {
        Body = body;
    }

    [Required]
    public RegisterCompanyRequestBody Body { get; }
}
