using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Endpoint.Auth.Login;
using RMS.Identity.Service.Api.Endpoint.SignUp;
using RMS.Identity.Service.Api.Middleware;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Api.Shared.Idempotency;
using RMS.Identity.Service.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(ApiErrorResponse.Create(
            "VALIDATION_ERROR",
            "Request validation failed.",
            details));
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddIdentityServiceInfrastructure(builder.Configuration);
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<LoginRequestValidator>();
builder.Services.AddScoped<LoginRequestValidationFilter>();
builder.Services.AddScoped<SignUpRequestValidator>();
builder.Services.AddScoped<SignUpRequestValidationFilter>();

var app = builder.Build();

app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { service = "RMS.Identity.Service", version = "1.0.0" }));

app.Run();

public partial class Program;
