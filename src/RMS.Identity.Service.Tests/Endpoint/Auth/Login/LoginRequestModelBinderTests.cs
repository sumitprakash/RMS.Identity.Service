using System.ComponentModel.DataAnnotations;
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
using RMS.Identity.Service.Api.Endpoint.Auth.Login;
using RMS.Identity.Service.Api.Shared.ModelBinding;

namespace RMS.Identity.Service.Tests.Endpoint.Auth.Login;

public sealed class LoginRequestModelBinderTests
{
    [Fact]
    public void Validate_WithEmailUsername_IsValid()
    {
        var body = new LoginRequestBody
        {
            Username = "alice@example.com",
            Password = "StrongPass@123"
        };

        var errors = Validate(body);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithAlphanumericUsername_IsInvalid()
    {
        var body = new LoginRequestBody
        {
            Username = "aliceuser",
            Password = "StrongPass@123"
        };

        var errors = Validate(body);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(LoginRequestBody.Username)));
    }

    [Fact]
    public async Task BindModelAsync_WithFlatJsonBody_CreatesLoginRequest()
    {
        var bindingContext = CreateBindingContext(
            """
            {
              "username": "alice@example.com",
              "password": "StrongPass@123"
            }
            """);
        var binder = CreateBinder();

        await binder.BindModelAsync(bindingContext);

        Assert.True(bindingContext.Result.IsModelSet);

        var request = Assert.IsType<LoginRequest>(bindingContext.Result.Model);
        Assert.Equal("alice@example.com", request.Body.Username);
        Assert.Equal("StrongPass@123", request.Body.Password);
    }

    [Fact]
    public async Task BindModelAsync_WithUnexpectedBodyProperty_AddsModelStateError()
    {
        var bindingContext = CreateBindingContext(
            """
            {
              "username": "alice@example.com",
              "password": "StrongPass@123",
              "unexpected": "value"
            }
            """);
        var binder = CreateBinder();

        await binder.BindModelAsync(bindingContext);

        Assert.False(bindingContext.Result.IsModelSet);
        Assert.False(bindingContext.ModelState.IsValid);
        Assert.Contains(nameof(LoginRequest.Body), bindingContext.ModelState.Keys);
    }

    private static ApiRequestModelBinder<LoginRequest> CreateBinder()
    {
        var jsonOptions = new JsonOptions();
        jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        return new ApiRequestModelBinder<LoginRequest>(Options.Create(jsonOptions));
    }

    private static IReadOnlyCollection<ValidationResult> Validate(object instance)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            validationResults,
            validateAllProperties: true);

        return validationResults;
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
        var metadata = metadataProvider.GetMetadataForType(typeof(LoginRequest));

        return DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            new CompositeValueProvider(),
            metadata,
            bindingInfo: null,
            modelName: "request");
    }
}
