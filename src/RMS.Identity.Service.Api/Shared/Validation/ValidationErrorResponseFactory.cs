using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RMS.Identity.Service.Api.Shared.ErrorHandling;

namespace RMS.Identity.Service.Api.Shared.Validation;

public static class ValidationErrorResponseFactory
{
    public static BadRequestObjectResult Create(ModelStateDictionary modelState)
    {
        var details = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Invalid value."
                        : error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(ApiErrorResponse.Create(
            ApiErrors.BadRequest.ValidationError,
            details));
    }
}
