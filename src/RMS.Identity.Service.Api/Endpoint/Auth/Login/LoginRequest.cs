using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.ModelBinding;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

[ModelBinder(BinderType = typeof(ApiRequestModelBinder<LoginRequest>))]
public sealed class LoginRequest
{
    [FromBody]
    [Required]
    public LoginRequestBody Body { get; set; } = default!;
}
