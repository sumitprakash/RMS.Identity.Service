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
using RMS.Identity.Service.Api.Endpoint.SignUp;

namespace RMS.Identity.Service.Tests.Endpoint.SignUp;

public sealed class SignUpRequestModelBinderTests
{
    [Fact]
    public async Task BindModelAsync_WithFlatJsonBody_CreatesSignUpRequest()
    {
        var bindingContext = CreateBindingContext(
            """
            {
              "emailAddress": "alice@example.com",
              "password": "StrongPass@123",
              "firstName": "Alice",
              "middleName": null,
              "lastName": "Example",
              "phoneNumber": "+919876543210"
            }
            """);
        var binder = CreateBinder();

        await binder.BindModelAsync(bindingContext);

        Assert.True(bindingContext.Result.IsModelSet);

        var request = Assert.IsType<SignUpRequest>(bindingContext.Result.Model);
        Assert.Equal("alice@example.com", request.Body.EmailAddress);
        Assert.Equal("StrongPass@123", request.Body.Password);
        Assert.Equal("Alice", request.Body.FirstName);
        Assert.Null(request.Body.MiddleName);
        Assert.Equal("Example", request.Body.LastName);
        Assert.Equal("+919876543210", request.Body.PhoneNumber);
    }

    [Fact]
    public async Task BindModelAsync_WithUnexpectedBodyProperty_AddsModelStateError()
    {
        var bindingContext = CreateBindingContext(
            """
            {
              "emailAddress": "alice@example.com",
              "password": "StrongPass@123",
              "firstName": "Alice",
              "lastName": "Example",
              "phoneNumber": "+919876543210",
              "unexpected": "value"
            }
            """);
        var binder = CreateBinder();

        await binder.BindModelAsync(bindingContext);

        Assert.False(bindingContext.Result.IsModelSet);
        Assert.False(bindingContext.ModelState.IsValid);
        Assert.Contains(nameof(SignUpRequest.Body), bindingContext.ModelState.Keys);
    }

    private static SignUpRequestModelBinder CreateBinder()
    {
        var jsonOptions = new JsonOptions();
        jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        return new SignUpRequestModelBinder(Options.Create(jsonOptions));
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
        var metadata = metadataProvider.GetMetadataForType(typeof(SignUpRequest));

        return DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            new CompositeValueProvider(),
            metadata,
            bindingInfo: null,
            modelName: "request");
    }
}
