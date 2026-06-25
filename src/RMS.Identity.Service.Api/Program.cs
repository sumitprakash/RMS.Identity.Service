using System.Text.Json;
using System.Threading.RateLimiting;
using RMS.Identity.Service.Api.Endpoint.Auth.Refresh;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using RMS.Identity.Service.Api.Endpoint.Auth.Login;
using RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;
using RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;
using RMS.Identity.Service.Api.Endpoint.SignUp;
using RMS.Identity.Service.Api.Middleware;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Api.Shared.Idempotency;
using RMS.Identity.Service.Api.Shared.Validation;
using RMS.Identity.Service.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 64 * 1024;
});

builder.Services
    .AddControllers(options =>
    {
        options.Filters.AddService<RequestValidationFilter>();
    })
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
            ApiErrors.BadRequest.ValidationError,
            details));
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);
    options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT bearer access token."
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            ApiErrorResponse.Create("429", "Too many authentication requests. Try again later."),
            cancellationToken);
    };
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddIdentityServiceInfrastructure(builder.Configuration);
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddSingleton<RequestValidationFilter>();
builder.Services.AddSingleton<IRequestValidator, LoginRequestValidator>();
builder.Services.AddSingleton<IRequestValidator, RefreshRequestValidator>();
builder.Services.AddSingleton<IRequestValidator, SignUpRequestValidator>();
builder.Services.AddSingleton<IRequestValidator, RegisterCompanyRequestValidator>();
builder.Services.AddSingleton<IRequestValidator, CreateCompanyUserRequestValidator>();
builder.Services.AddScoped<IAccessTokenUserResolver, JwtAccessTokenUserResolver>();
builder.Services.AddScoped<ICompanyAccessAuthorizer, CompanyAccessAuthorizer>();
builder.Services.AddScoped<IPlatformAdminAuthorizer, PlatformAdminAuthorizer>();

var app = builder.Build();

app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.UseRateLimiter();
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
