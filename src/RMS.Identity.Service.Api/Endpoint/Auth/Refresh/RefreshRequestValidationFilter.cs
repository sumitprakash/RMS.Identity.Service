using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

public sealed class RefreshRequestValidationFilter : IActionFilter
{
    private readonly RefreshRequestValidator _validator;

    public RefreshRequestValidationFilter(RefreshRequestValidator validator)
    {
        _validator = validator;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ActionArguments.TryGetValue("request", out var requestValue)
            || requestValue is not RefreshRequest request)
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
