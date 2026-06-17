using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using RMS.Identity.Service.Api.Endpoint.Auth.Refresh;
using RMS.Identity.Service.Api.Shared.ErrorHandling;

namespace RMS.Identity.Service.Tests.Endpoint.Auth.Refresh;

public sealed class RefreshRequestValidationFilterTests
{
    [Fact]
    public void OnActionExecuting_WithInvalidRequest_ReturnsBadRequest()
    {
        var filter = new RefreshRequestValidationFilter(new RefreshRequestValidator());
        var context = CreateContext(new RefreshRequest(new RefreshRequestBody
        {
            RefreshToken = " "
        }));

        filter.OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var body = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("400", body.Code);
        Assert.Equal("Refresh token is required.", body.Message);
    }

    [Fact]
    public void OnActionExecuting_WithValidRequest_AllowsActionToContinue()
    {
        var filter = new RefreshRequestValidationFilter(new RefreshRequestValidator());
        var context = CreateContext(new RefreshRequest(new RefreshRequestBody
        {
            RefreshToken = "refresh-token"
        }));

        filter.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    private static ActionExecutingContext CreateContext(RefreshRequest request)
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
