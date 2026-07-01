using System.Net;
using System.Net.Http.Json;
using RMS.Identity.Service.Tests.Shared;

namespace RMS.Identity.Service.Tests.Endpoint.Auth;

public sealed class AuthenticationPipelineTests
{
    [Fact]
    public async Task ProtectedGetEndpoint_WithoutBearerToken_ReturnsUnauthorized()
    {
        await using var factory = new TestDatabaseWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/companies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedMutatingEndpoint_WithoutBearerToken_ReturnsUnauthorizedBeforeIdempotencyValidation()
    {
        await using var factory = new TestDatabaseWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/companies",
            new
            {
                legalName = "Example Retail Pvt Ltd"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicEndpoint_WithoutBearerToken_DoesNotRequireAuthentication()
    {
        await using var factory = new TestDatabaseWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
