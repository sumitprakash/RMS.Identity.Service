using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public sealed class SignUpRequestValidationFilter : IActionFilter
{
    private readonly SignUpRequestValidator _validator;

    public SignUpRequestValidationFilter(SignUpRequestValidator validator)
    {
        _validator = validator;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ActionArguments.TryGetValue("body", out var requestBody)
            || requestBody is not SignUpRequestBody body)
        {
            return;
        }

        try
        {
            _validator.Validate(body);
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
