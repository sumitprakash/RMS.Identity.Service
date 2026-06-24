using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.ModelBinding;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

[ModelBinder(BinderType = typeof(ApiRequestModelBinder<RefreshRequest>))]
public sealed class RefreshRequest
{
    [FromBody]
    [Required]
    public RefreshRequestBody Body { get; set; } = default!;
}
