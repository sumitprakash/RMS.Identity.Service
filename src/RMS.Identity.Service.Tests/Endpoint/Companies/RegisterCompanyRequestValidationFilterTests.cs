using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Api.Shared.Validation;

namespace RMS.Identity.Service.Tests.Endpoint.Companies;

public sealed class RegisterCompanyRequestValidationFilterTests
{
    [Fact]
    public void OnActionExecuting_WithValidRequest_AllowsActionToContinue()
    {
        var filter = CreateFilter();
        var context = CreateContext(new RegisterCompanyRequest
        {
            Body = CreateValidBody()
        });

        filter.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_WithInvalidGstin_ReturnsBadRequest()
    {
        var filter = CreateFilter();
        var context = CreateContext(new RegisterCompanyRequest
        {
            Body = CreateValidBody("bad-gstin")
        });

        filter.OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var body = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(ServiceIdentity.Code, body.ServiceCode);
        Assert.Equal("400", body.Code);
        Assert.Equal("GSTIN must be a valid GSTIN.", body.Message);
    }

    [Fact]
    public void OnActionExecuting_WithInvalidCountryLength_ReturnsBadRequest()
    {
        var body = CreateValidBody();
        var filter = CreateFilter();
        var context = CreateContext(new RegisterCompanyRequest
        {
            Body = new RegisterCompanyRequestBody
            {
                LegalName = body.LegalName,
                TradeName = body.TradeName,
                Gstin = body.Gstin,
                ContactEmailAddress = body.ContactEmailAddress,
                ContactPhoneNumber = body.ContactPhoneNumber,
                AddressLine1 = body.AddressLine1,
                City = body.City,
                State = body.State,
                PostalCode = body.PostalCode,
                Country = "IND"
            }
        });

        filter.OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var response = Assert.IsType<ApiErrorResponse>(result.Value);
        Assert.Equal("Company country must be a two-letter country code.", response.Message);
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

    private static RequestValidationFilter CreateFilter() =>
        new([new RegisterCompanyRequestValidator()]);
}
