using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

namespace RMS.Identity.Service.Tests.Endpoint.Auth.Refresh;

public sealed class RefreshRequestModelBinderTests
{
    [Fact]
    public async Task BindModelAsync_WithFlatJsonBody_CreatesRefreshRequest()
    {
        var bindingContext = CreateBindingContext(
            """
            {
              "refreshToken": "refresh-token"
            }
            """);
        var binder = CreateBinder();

        await binder.BindModelAsync(bindingContext);

        Assert.True(bindingContext.Result.IsModelSet);

        var request = Assert.IsType<RefreshRequest>(bindingContext.Result.Model);
        Assert.Equal("refresh-token", request.Body.RefreshToken);
    }

    [Fact]
    public async Task BindModelAsync_WithUnexpectedBodyProperty_AddsModelStateError()
    {
        var bindingContext = CreateBindingContext(
            """
            {
              "refreshToken": "refresh-token",
              "unexpected": "value"
            }
            """);
        var binder = CreateBinder();

        await binder.BindModelAsync(bindingContext);

        Assert.False(bindingContext.Result.IsModelSet);
        Assert.False(bindingContext.ModelState.IsValid);
        Assert.Contains(nameof(RefreshRequest.Body), bindingContext.ModelState.Keys);
    }

    private static RefreshRequestModelBinder CreateBinder()
    {
        var jsonOptions = new JsonOptions();
        jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        return new RefreshRequestModelBinder(Options.Create(jsonOptions));
    }

    private static ModelBindingContext CreateBindingContext(string body)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(RefreshRequest));

        return DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            new CompositeValueProvider(),
            metadata,
            bindingInfo: null,
            modelName: "request");
    }
}
