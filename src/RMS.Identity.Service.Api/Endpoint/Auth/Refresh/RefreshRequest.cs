using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

[ModelBinder(BinderType = typeof(RefreshRequestModelBinder))]
public sealed class RefreshRequest
{
    public RefreshRequest(RefreshRequestBody body)
    {
        Body = body;
    }

    [Required]
    public RefreshRequestBody Body { get; }
}
