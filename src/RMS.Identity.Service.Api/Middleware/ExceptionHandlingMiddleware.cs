using System.Text.Json;
using RMS.Identity.Service.Api.Contracts;
using RMS.Identity.Service.Domain.Exceptions;

namespace RMS.Identity.Service.Api.Middleware;

internal sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/json";

            var payload = new ErrorResponse(exception.Code, exception.Message, exception.Details);
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new ErrorResponse("internal_error", "An unexpected error occurred");
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
        }
    }
}
