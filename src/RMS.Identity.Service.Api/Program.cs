using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using RMS.Identity.Service.Api.Endpoint.SignUp;
using RMS.Identity.Service.Api.Logging;
using RMS.Identity.Service.Api.Middleware;
using RMS.Identity.Service.Api.RateLimiting;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Api.Shared.Correlation;
using RMS.Identity.Service.Api.Shared.ErrorHandling;
using RMS.Identity.Service.Api.Shared.Forwarding;
using RMS.Identity.Service.Api.Shared.Idempotency;
using RMS.Identity.Service.Api.Shared.Validation;
using RMS.Identity.Service.Api.Swagger;
using RMS.Identity.Service.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFileLogger(builder.Configuration, builder.Environment);
builder.Logging.AddDatabaseLogger(builder.Configuration);
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
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(
            namingPolicy: null,
            allowIntegerValues: false));
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
        ValidationErrorResponseFactory.Create(context.ModelState);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services
    .AddAuthentication(BearerAuthenticationDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, BearerAuthenticationHandler>(
        BearerAuthenticationDefaults.Scheme,
        options => { });
builder.Services.AddAuthorization();
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
    options.OperationFilter<IdentityServiceOperationFilter>();
});

builder.Services.AddRateLimiter(_ => { });

builder.Services.AddOptions<GlobalRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(GlobalRateLimitOptions.SectionName))
    .Validate(options => options.PermitLimit > 0, "Global rate limit permit limit must be greater than zero.")
    .Validate(options => options.WindowSeconds > 0, "Global rate limit window must be greater than zero.")
    .Validate(options => options.QueueLimit >= 0, "Global rate limit queue limit cannot be negative.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.RejectionMessage), "Global rate limit rejection message is required.")
    .ValidateOnStart();
builder.Services.AddOptions<RateLimiterOptions>()
    .Configure<IOptions<GlobalRateLimitOptions>>((options, globalRateLimitOptionsAccessor) =>
    {
        var globalRateLimitOptions = globalRateLimitOptionsAccessor.Value;

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(
                ApiErrorResponse.Create(
                    "429",
                    globalRateLimitOptions.RejectionMessage,
                    correlationTraceId: CorrelationTraceContext.GetCorrelationTraceId(context.HttpContext)),
                cancellationToken);
        };
        options.AddPolicy(GlobalRateLimitPolicy.Name, context =>
        {
            var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return globalRateLimitOptions.Enabled
                ? RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = globalRateLimitOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(globalRateLimitOptions.WindowSeconds),
                        QueueLimit = globalRateLimitOptions.QueueLimit,
                        AutoReplenishment = globalRateLimitOptions.AutoReplenishment
                    })
                : RateLimitPartition.GetNoLimiter(partitionKey);
        });
    });
builder.Services.AddOptions<ForwardedHeadersSettings>()
    .Bind(builder.Configuration.GetSection(ForwardedHeadersSettings.SectionName))
    .Validate(options => !options.Enabled || options.ForwardLimit > 0, "Forwarded headers forward limit must be greater than zero when enabled.")
    .Validate(ForwardedHeadersSetup.HasValidKnownProxies, "Forwarded headers known proxies must be valid IP addresses.")
    .Validate(ForwardedHeadersSetup.HasValidKnownNetworks, "Forwarded headers known networks must use CIDR notation, for example 10.0.0.0/8.")
    .ValidateOnStart();
builder.Services.AddOptions<ForwardedHeadersOptions>()
    .Configure<IOptions<ForwardedHeadersSettings>>((options, settings) =>
    {
        ForwardedHeadersSetup.ApplyTo(settings.Value, options);
    });
builder.Services.AddOptions<CorrelationTraceOptions>()
    .Bind(builder.Configuration.GetSection(CorrelationTraceOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ResponseHeaderName), "Correlation trace response header name is required.")
    .ValidateOnStart();
builder.Services.AddOptions<RMS.Identity.Service.Api.Logging.File.FileLoggerOptions>()
    .Bind(builder.Configuration.GetSection(RMS.Identity.Service.Api.Logging.File.FileLoggerOptions.SectionName))
    .Validate(options => options.QueueCapacity > 0, "File logger queue capacity must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddOptions<RMS.Identity.Service.Api.Logging.Database.DatabaseLoggerOptions>()
    .Bind(builder.Configuration.GetSection(RMS.Identity.Service.Api.Logging.Database.DatabaseLoggerOptions.SectionName))
    .Validate(options => options.QueueCapacity > 0, "Database logger queue capacity must be greater than zero.")
    .Validate(options => options.CommandTimeoutSeconds > 0, "Database logger command timeout must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddIdentityServiceInfrastructure(builder.Configuration);
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddSingleton<RequestValidationFilter>();
builder.Services.AddSingleton<IRequestValidator, SignUpRequestValidator>();
builder.Services.AddScoped<IAccessTokenUserResolver, JwtAccessTokenUserResolver>();
builder.Services.AddScoped<ICompanyAccessAuthorizer, CompanyAccessAuthorizer>();
builder.Services.AddScoped<IPlatformAdminAuthorizer, PlatformAdminAuthorizer>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationTraceMiddleware>();
app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers()
    .RequireAuthorization()
    .RequireRateLimiting(GlobalRateLimitPolicy.Name);
app.MapGet("/", () => Results.Ok(new { service = "RMS.Identity.Service", version = "1.0.0" }))
    .RequireRateLimiting(GlobalRateLimitPolicy.Name);

app.Run();

public partial class Program;
