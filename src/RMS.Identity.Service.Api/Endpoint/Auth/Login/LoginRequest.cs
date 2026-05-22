using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

[ModelBinder(BinderType = typeof(LoginRequestModelBinder))]
public sealed class LoginRequest
{
    public LoginRequest(LoginRequestBody body)
    {
        Body = body;
    }

    [Required]
    public LoginRequestBody Body { get; }
}
