using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Shared.Errors;
using System.Text.Json;

namespace RMS.Identity.Service.Api.Middleware;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger,
        IOptions<JsonOptions> jsonOptions)
    {
        _next = next;
        _logger = logger;
        _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ServiceException exception)
        {
            context.Response.StatusCode = (int)exception.StatusCode;
            context.Response.ContentType = "application/json";

            var response = ApiErrorResponse.Create(exception.Code, exception.Message, exception.Details);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing request.");

            var internalError = ServiceErrors.General.UnhandledException;

            context.Response.StatusCode = (int)ServiceStatusErrorCodes.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = ApiErrorResponse.Create(
                internalError.ToResponseCode((int)ServiceStatusErrorCodes.InternalServerError),
                internalError.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));
        }
    }
}
