using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Shared.Validation;

public sealed class RequestValidationFilter : IActionFilter
{
    private readonly IReadOnlyCollection<IRequestValidator> _validators;

    public RequestValidationFilter(IEnumerable<IRequestValidator> validators)
    {
        _validators = validators.ToArray();
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            foreach (var validator in _validators.Where(validator => validator.RequestType.IsInstanceOfType(argument)))
            {
                try
                {
                    validator.Validate(argument);
                }
                catch (ServiceException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
                {
                    context.Result = new BadRequestObjectResult(ApiErrorResponse.Create(
                        exception.Code,
                        exception.Message,
                        exception.Details));
                    return;
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
