using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using RMS.Identity.Service.Api.Shared.Idempotency;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RMS.Identity.Service.Api.Swagger;

public sealed class IdentityServiceOperationFilter : IOperationFilter
{
    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/api/v1/signup",
        "/api/v1/auth/login",
        "/api/v1/auth/refresh",
        "/api/v1/users/verify-email"
    };

    private static readonly HashSet<string> IdempotentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = GetPath(context.ApiDescription);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (RequiresBearer(path))
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearerAuth", context.Document)] = new List<string>()
            });
        }

        if (RequiresIdempotency(context.ApiDescription.HttpMethod, path))
        {
            operation.Parameters ??= new List<IOpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = IdempotencyHttpHeaders.HeaderName,
                In = ParameterLocation.Header,
                Required = true,
                Description = "Unique UUID for safely retrying mutating requests.",
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Format = "uuid"
                }
            });
        }
    }

    private static bool RequiresBearer(string path) =>
        !PublicPaths.Contains(path);

    private static bool RequiresIdempotency(string? method, string path) =>
        !string.IsNullOrWhiteSpace(method)
        && IdempotentMethods.Contains(method)
        && !path.Equals("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase)
        && !path.Equals("/api/v1/auth/refresh", StringComparison.OrdinalIgnoreCase)
        && !path.Equals("/api/v1/users/verify-email", StringComparison.OrdinalIgnoreCase);

    private static string GetPath(ApiDescription apiDescription) =>
        "/" + (apiDescription.RelativePath ?? string.Empty).Split('?')[0];
}
