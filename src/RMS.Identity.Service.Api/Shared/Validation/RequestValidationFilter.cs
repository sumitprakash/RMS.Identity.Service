using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
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
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is not IValidatableRequest request)
            {
                continue;
            }

            if (!_validators.TryGetValue(request.GetType(), out var validator))
            {
                throw new InvalidOperationException(
                    $"No request validator is registered for request type '{request.GetType().FullName}'.");
            }

            try
            {
                validator.Validate(request);
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

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
