using Microsoft.AspNetCore.Mvc;

namespace RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;

[ModelBinder(BinderType = typeof(CreateCompanyUserRequestModelBinder))]
public sealed class CreateCompanyUserRequest
{
    public CreateCompanyUserRequest(CreateCompanyUserRequestBody body)
    {
        Body = body;
    }

    public CreateCompanyUserRequestBody Body { get; }
}
