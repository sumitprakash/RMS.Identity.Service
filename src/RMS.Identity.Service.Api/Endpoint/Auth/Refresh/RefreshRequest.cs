using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.ModelBinding;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

[ModelBinder(BinderType = typeof(ApiRequestModelBinder<RefreshRequest>))]
public sealed class RefreshRequest : IValidatableRequest
{
    [FromBody]
    [Required]
    public RefreshRequestBody Body { get; set; } = default!;
}
