using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.ModelBinding;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

[ModelBinder(BinderType = typeof(ApiRequestModelBinder<LoginRequest>))]
public sealed class LoginRequest : IValidatableRequest
{
    [FromBody]
    [Required]
    public LoginRequestBody Body { get; set; } = default!;
}
