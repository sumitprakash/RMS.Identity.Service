using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.ModelBinding;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

[ModelBinder(BinderType = typeof(ApiRequestModelBinder<SignUpRequest>))]
public sealed class SignUpRequest : IValidatableRequest
{
    [FromBody]
    [Required]
    public SignUpRequestBody Body { get; set; } = default!;
}
