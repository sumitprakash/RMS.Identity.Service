using Microsoft.AspNetCore.Mvc.Filters;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Shared.Validation;

public sealed class RequestValidationFilter : IActionFilter
{
    private readonly IReadOnlyDictionary<Type, IRequestValidator> _validators;

    public RequestValidationFilter(IEnumerable<IRequestValidator> validators)
    {
        _validators = validators.ToDictionary(validator => validator.RequestType);
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = ValidationErrorResponseFactory.Create(context.ModelState);
            return;
        }

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            if (!_validators.TryGetValue(argument.GetType(), out var validator))
            {
                continue;
            }

            try
            {
                validator.Validate(argument);
            }
            catch (ServiceException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
            {
                context.ModelState.AddModelError(string.Empty, exception.Message);
                context.Result = ValidationErrorResponseFactory.Create(context.ModelState);
                return;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
