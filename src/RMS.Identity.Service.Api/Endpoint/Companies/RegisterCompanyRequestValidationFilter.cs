using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Endpoint.Companies;

public sealed class RegisterCompanyRequestValidationFilter : IActionFilter
{
    private readonly RegisterCompanyRequestValidator _validator;

    public RegisterCompanyRequestValidationFilter(RegisterCompanyRequestValidator validator)
    {
        _validator = validator;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ActionArguments.TryGetValue("request", out var requestValue)
            || requestValue is not RegisterCompanyRequest request)
        {
            return;
        }

        try
        {
            _validator.Validate(request);
        }
        catch (ServiceException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
        {
            context.Result = new BadRequestObjectResult(ApiErrorResponse.Create(
                exception.Code,
                exception.Message,
                exception.Details));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
