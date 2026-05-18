using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

[ModelBinder(BinderType = typeof(SignUpRequestModelBinder))]
public sealed class SignUpRequest
{
    public SignUpRequest(SignUpRequestBody body)
    {
        Body = body;
    }

    [Required]
    public SignUpRequestBody Body { get; }
}
