using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using RMS.Identity.Service.Api.Endpoint.Companies;
using RMS.Identity.Service.Api.Shared.ErrorHandling;

namespace RMS.Identity.Service.Tests.Endpoint.Companies;

public sealed class RegisterCompanyRequestValidationFilterTests
{
    [Fact]
    public void OnActionExecuting_WithValidRequest_AllowsActionToContinue()
    {
        var filter = new RegisterCompanyRequestValidationFilter(new RegisterCompanyRequestValidator());
        var context = CreateContext(new RegisterCompanyRequest(CreateValidBody()));

        filter.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_WithInvalidGstin_ReturnsBadRequest()
    {
        var filter = new RegisterCompanyRequestValidationFilter(new RegisterCompanyRequestValidator());
        var context = CreateContext(new RegisterCompanyRequest(CreateValidBody("bad-gstin")));

        filter.OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var body = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("VALIDATION_ERROR", body.Code);
        Assert.Equal("GSTIN must be a valid GSTIN.", body.Message);
    }

    private static ActionExecutingContext CreateContext(RegisterCompanyRequest request)
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

    private static RegisterCompanyRequestBody CreateValidBody(string gstin = "29ABCDE1234F1Z5") =>
        new()
        {
            LegalName = "Example Retail Pvt Ltd",
            TradeName = "Example Retail",
            Gstin = gstin,
            ContactEmailAddress = "accounts@example.com",
            ContactPhoneNumber = "+919876543211",
            AddressLine1 = "1 Main Road",
            City = "Bengaluru",
            State = "Karnataka",
            PostalCode = "560001",
            Country = "IN"
        };
}
