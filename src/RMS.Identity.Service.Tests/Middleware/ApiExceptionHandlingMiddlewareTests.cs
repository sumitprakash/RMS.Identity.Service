using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Api.Middleware;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Tests.Middleware;

public sealed class ApiExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithStructuredServiceException_WritesStructuredErrorResponse()
    {
        var middleware = CreateMiddleware(_ =>
            throw new InternalServerErrorException(ServiceErrorDefinitions.General.DatabaseTransactionMissing));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        var response = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("500-1-2", response.Code);
        Assert.Equal("Database transaction is not available.", response.Message);
    }

    [Fact]
    public async Task InvokeAsync_WithUnhandledException_WritesInternalErrorResponse()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Failure."));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        var response = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("500-1-1", response.Code);
        Assert.Equal("An unexpected error occurred.", response.Message);
    }

    private static ApiExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        var jsonOptions = new JsonOptions();
        jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        return new ApiExceptionHandlingMiddleware(
            next,
            NullLogger<ApiExceptionHandlingMiddleware>.Instance,
            Options.Create(jsonOptions));
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ApiErrorResponse> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<ApiErrorResponse>(
                json,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Expected API error response.");
    }
}
