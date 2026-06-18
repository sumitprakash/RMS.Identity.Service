using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Application.Shared.Errors;
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
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(exception, "Unable to write service error response because the response has already started.");
                throw;
            }

            await HandleServiceExceptionAsync(context, exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing request.");

            if (context.Response.HasStarted)
            {
                throw;
            }

            var internalError = new InternalServerErrorException(ServiceErrorDefinitions.General.UnhandledException);
            await HandleServiceExceptionAsync(context, internalError);
        }
    }

    private Task HandleServiceExceptionAsync(HttpContext context, ServiceException exception)
    {
        var response = ApiErrorResponse.Create(exception.Code, exception.Message, exception.Details);

        return WriteErrorResponseAsync(context, exception.StatusCode, response);
    }

    private Task WriteErrorResponseAsync(HttpContext context, int statusCode, ApiErrorResponse response)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));
    }
}
