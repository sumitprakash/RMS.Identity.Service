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
using RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;
using RMS.Identity.Service.Api.Shared.ModelBinding;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

namespace RMS.Identity.Service.Tests.Endpoint.Companies;

public sealed class CreateCompanyUserRequestModelBinderTests
{
    [Fact]
    public async Task BindModelAsync_WithRouteAndFlatJsonBody_CreatesCreateCompanyUserRequest()
    {
        var companyUuid = Guid.NewGuid();
        var bindingContext = CreateBindingContext(
            companyUuid,
            """
            {
              "username": "alice@example.com",
              "displayName": "Alice Example",
              "companyRole": "ADMIN"
            }
            """);
        var binder = CreateBinder();

        await binder.BindModelAsync(bindingContext);

        Assert.True(bindingContext.Result.IsModelSet);

        var request = Assert.IsType<CreateCompanyUserRequest>(bindingContext.Result.Model);
        Assert.Equal(companyUuid, request.CompanyUuid);
        Assert.Equal("alice@example.com", request.Body.Username);
        Assert.Equal("Alice Example", request.Body.DisplayName);
        Assert.Equal(CompanyRole.Admin, request.Body.CompanyRole);
    }

    private static ApiRequestModelBinder<CreateCompanyUserRequest> CreateBinder()
    {
        var jsonOptions = new JsonOptions();
        jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(
            namingPolicy: null,
            allowIntegerValues: false));

        return new ApiRequestModelBinder<CreateCompanyUserRequest>(Options.Create(jsonOptions));
    }

    private static ModelBindingContext CreateBindingContext(Guid companyUuid, string body)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var routeData = new RouteData();
        routeData.Values["companyUuid"] = companyUuid.ToString();

        var actionContext = new ActionContext(
            httpContext,
            routeData,
            new ActionDescriptor(),
            new ModelStateDictionary());
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(CreateCompanyUserRequest));

        return DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            new CompositeValueProvider(),
            metadata,
            bindingInfo: null,
            modelName: "request");
    }
}
