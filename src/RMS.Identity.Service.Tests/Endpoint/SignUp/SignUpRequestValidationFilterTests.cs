using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using RMS.Identity.Service.Api.Endpoint.SignUp;
using RMS.Identity.Service.Api.Shared.ErrorHandling;

namespace RMS.Identity.Service.Tests.Endpoint.SignUp;

public sealed class SignUpRequestValidationFilterTests
{
    [Fact]
    public void OnActionExecuting_WithInvalidRequest_ReturnsBadRequest()
    {
        var filter = new SignUpRequestValidationFilter(new SignUpRequestValidator());
        var context = CreateContext(new SignUpRequestBody
        {
            EmailAddress = "alice@example.com",
            Password = "StrongPass@123",
            FirstName = " ",
            LastName = "Example",
            PhoneNumber = "+919876543210"
        });

        filter.OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var body = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("VALIDATION_ERROR", body.Code);
        Assert.Equal("First name is required.", body.Message);
    }

    [Fact]
    public void OnActionExecuting_WithValidRequest_AllowsActionToContinue()
    {
        var filter = new SignUpRequestValidationFilter(new SignUpRequestValidator());
        var context = CreateContext(new SignUpRequestBody
        {
            EmailAddress = "alice@example.com",
            Password = "StrongPass@123",
            FirstName = "Alice",
            LastName = "Example",
            PhoneNumber = "+919876543210"
        });

        filter.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    private static ActionExecutingContext CreateContext(SignUpRequestBody body)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { ["body"] = body },
            new object());
    }
}
