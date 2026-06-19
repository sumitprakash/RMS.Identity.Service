using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using RMS.Identity.Service.Api.Endpoint.Auth.Login;
using RMS.Identity.Service.Api.Shared.ErrorHandling;

namespace RMS.Identity.Service.Tests.Endpoint.Auth.Login;

public sealed class LoginRequestValidationFilterTests
{
    [Fact]
    public void OnActionExecuting_WithInvalidRequest_ReturnsBadRequest()
    {
        var filter = new LoginRequestValidationFilter(new LoginRequestValidator());
        var context = CreateContext(new LoginRequest(new LoginRequestBody
        {
            Username = "not-an-email",
            Password = "StrongPass@123"
        }));

        filter.OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var body = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("400", body.Code);
        Assert.Equal("Username must be a valid email address.", body.Message);
    }

    [Fact]
    public void OnActionExecuting_WithValidRequest_AllowsActionToContinue()
    {
        var filter = new LoginRequestValidationFilter(new LoginRequestValidator());
        var context = CreateContext(new LoginRequest(new LoginRequestBody
        {
            Username = "alice@example.com",
            Password = "StrongPass@123"
        }));

        filter.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    private static ActionExecutingContext CreateContext(LoginRequest request)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { ["request"] = request },
            new object());
    }
}
