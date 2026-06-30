using System.Net;
using System.Text.Json;
using RMS.Identity.Service.Api.Shared.Idempotency;
using RMS.Identity.Service.Tests.Shared;

namespace RMS.Identity.Service.Tests.Endpoint.Swagger;

public sealed class SwaggerDocumentTests
{
    [Fact]
    public async Task SwaggerDocument_RegistersBearerAuthenticationScheme()
    {
        await using var factory = new TestDatabaseWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var content = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(content);
        var bearerScheme = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("bearerAuth");
        Assert.Equal("http", bearerScheme.GetProperty("type").GetString());
        Assert.Equal("bearer", bearerScheme.GetProperty("scheme").GetString());
    }

    [Fact]
    public async Task SwaggerDocument_AnnotatesProtectedMutatingOperations()
    {
        await using var factory = new TestDatabaseWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var content = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(content);
        var postCompany = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/companies")
            .GetProperty("post");

        Assert.True(HasBearerSecurity(postCompany));
        var idempotencyParameter = GetParameter(postCompany, IdempotencyHttpHeaders.HeaderName);
        Assert.Equal("header", idempotencyParameter.GetProperty("in").GetString());
        Assert.True(idempotencyParameter.GetProperty("required").GetBoolean());
        Assert.Equal("uuid", idempotencyParameter.GetProperty("schema").GetProperty("format").GetString());
    }

    [Fact]
    public async Task SwaggerDocument_DoesNotAnnotateAuthLoginWithBearerOrIdempotency()
    {
        await using var factory = new TestDatabaseWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var content = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(content);
        var login = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/auth/login")
            .GetProperty("post");

        Assert.False(HasBearerSecurity(login));
        Assert.False(TryGetParameter(login, IdempotencyHttpHeaders.HeaderName, out _));
    }

    private static bool HasBearerSecurity(JsonElement operation)
    {
        if (!operation.TryGetProperty("security", out var securityRequirements))
        {
            return false;
        }

        return securityRequirements
            .EnumerateArray()
            .Any(requirement => requirement.TryGetProperty("bearerAuth", out _));
    }

    private static JsonElement GetParameter(JsonElement operation, string name)
    {
        Assert.True(TryGetParameter(operation, name, out var parameter));
        return parameter;
    }

    private static bool TryGetParameter(JsonElement operation, string name, out JsonElement parameter)
    {
        parameter = default;
        if (!operation.TryGetProperty("parameters", out var parameters))
        {
            return false;
        }

        foreach (var candidate in parameters.EnumerateArray())
        {
            if (string.Equals(candidate.GetProperty("name").GetString(), name, StringComparison.Ordinal))
            {
                parameter = candidate;
                return true;
            }
        }

        return false;
    }
}
