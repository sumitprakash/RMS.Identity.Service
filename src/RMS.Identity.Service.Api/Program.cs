using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RMS.Identity.Service.Api.Contracts;
using RMS.Identity.Service.Api.Extensions;
using RMS.Identity.Service.Api.Middleware;
using RMS.Identity.Service.Application;
using RMS.Identity.Service.Application.Identity;
using RMS.Identity.Service.Application.Identity.Requests;
using RMS.Identity.Service.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ResolveSigningKey(jwtSection))),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Retail Identity Service API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "RMS.Identity.Service", status = "ok" }))
    .WithTags("System");

app.MapPost("/api/v1/signup", async (
    SignupRequest request,
    HttpRequest httpRequest,
    SignUpUserUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(
        new SignUpUserCommand(
            request.Username,
            request.Password,
            request.DisplayName,
            request.Phone,
            httpRequest.Headers["Idempotency-Key"].FirstOrDefault()),
        cancellationToken);

    return Results.Created("/api/v1/users/" + result.UserUuid, result.ToResponse());
})
    .WithName("SignUp")
    .WithTags("Auth")
    .Produces<UserResponse>(StatusCodes.Status201Created)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

app.MapPost("/api/v1/users/verify-email", async (
    VerifyEmailRequest request,
    VerifyEmailUseCase useCase,
    CancellationToken cancellationToken) =>
{
    await useCase.ExecuteAsync(new VerifyEmailCommand(request.Token), cancellationToken);
    return Results.Ok();
})
    .WithTags("Auth")
    .Produces(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

app.MapPost("/api/v1/auth/login", async (
    LoginRequest request,
    LoginUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(
        new LoginCommand(request.Username, request.Password, request.CompanyUuid),
        cancellationToken);

    return Results.Ok(result.ToResponse());
})
    .WithTags("Auth")
    .Produces<LoginResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

app.MapPost("/api/v1/auth/refresh", async (
    RefreshRequest request,
    RefreshTokenUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
    return Results.Ok(result.ToResponse());
})
    .WithTags("Auth")
    .Produces<RefreshResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

app.MapPost("/api/v1/users", () =>
        Results.Json(
            new ErrorResponse("not_implemented", "Tenant user creation is not implemented yet in this step"),
            statusCode: StatusCodes.Status501NotImplemented))
    .RequireAuthorization()
    .WithTags("Users")
    .Produces<ErrorResponse>(StatusCodes.Status501NotImplemented);

app.MapGet("/api/v1/users/{userUuid:guid}", async (
    Guid userUuid,
    HttpContext httpContext,
    GetUserUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(
        new GetUserQuery(userUuid, httpContext.User.GetCompanyUuid()),
        cancellationToken);

    return Results.Ok(result.ToResponse());
})
    .RequireAuthorization()
    .WithTags("Users")
    .Produces<UserResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

app.MapGet("/api/v1/tenants/{companyUuid:guid}", async (
    Guid companyUuid,
    HttpContext httpContext,
    GetCompanyUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(
        new GetCompanyQuery(companyUuid, httpContext.User.GetCompanyUuid()),
        cancellationToken);

    return Results.Ok(result.ToResponse());
})
    .RequireAuthorization()
    .WithTags("Tenants")
    .Produces<CompanyResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

app.Run();

static string ResolveSigningKey(IConfigurationSection jwtSection)
{
    var directValue = jwtSection["SigningKey"];
    if (!string.IsNullOrWhiteSpace(directValue))
    {
        return directValue;
    }

    var envVarName = jwtSection["SigningKeyEnvVar"];
    if (!string.IsNullOrWhiteSpace(envVarName))
    {
        var envValue = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }
    }

    throw new InvalidOperationException("JWT signing key is not configured.");
}
