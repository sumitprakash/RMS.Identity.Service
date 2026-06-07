using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Api.Shared.ErrorHandling;

namespace RMS.Identity.Service.Api.Endpoint.Companies;

public sealed class CreateCompanyUserRequestValidationFilter : IActionFilter
{
    private readonly CreateCompanyUserRequestValidator _validator;

    public CreateCompanyUserRequestValidationFilter(CreateCompanyUserRequestValidator validator)
    {
        _validator = validator;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ActionArguments.TryGetValue("request", out var requestValue)
            || requestValue is not CreateCompanyUserRequest request)
        {
            return;
        }

        try
        {
            _validator.Validate(request);
        }
        catch (ServiceException exception)
        {
            context.Result = new BadRequestObjectResult(ApiErrorResponse.Create(exception.Code, exception.Message));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
