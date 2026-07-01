using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Api.RateLimiting;

namespace RMS.Identity.Service.Tests.Endpoint.RateLimiting;

public sealed class ForwardedHeadersRateLimitTests
{
    [Fact]
    public async Task GlobalRateLimit_UsesForwardedClientAddress_WhenProxyIsTrusted()
    {
        await using var factory = new TrustedProxyRateLimitedWebApplicationFactory();
        var rateLimitOptions = factory.Services.GetRequiredService<IOptions<GlobalRateLimitOptions>>().Value;
        Assert.True(rateLimitOptions.Enabled);
        Assert.Equal(1, rateLimitOptions.PermitLimit);
        var endpointDataSource = factory.Services.GetRequiredService<EndpointDataSource>();
        Assert.Contains(endpointDataSource.Endpoints, endpoint =>
            endpoint.DisplayName?.Contains("LoginController.PostAsync", StringComparison.Ordinal) == true
            && endpoint.Metadata.Any(metadata => metadata.GetType().Name.Contains("RateLimiting", StringComparison.Ordinal)));

        using var client = factory.CreateClient();

        using var firstResponse = await PostLoginRequestAsync(client, "203.0.113.10");
        using var secondResponse = await PostLoginRequestAsync(client, "203.0.113.11");
        using var repeatedFirstClientResponse = await PostLoginRequestAsync(client, "203.0.113.10");

        Assert.Equal(HttpStatusCode.BadRequest, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, repeatedFirstClientResponse.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostLoginRequestAsync(HttpClient client, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { })
        };
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");

        return await client.SendAsync(request);
    }

    private sealed class TrustedProxyRateLimitedWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=rms_identity;User ID=rms_user;Password=12345678;SslMode=None;Connection Timeout=2;",
                    ["Jwt:SigningKey"] = "replace-this-development-signing-key-with-a-secure-secret",
                    ["Jwt:SigningKeyEnvVar"] = string.Empty,
                    ["FileLogging:Enabled"] = "false",
                    ["DatabaseLogging:Enabled"] = "false",
                    ["RateLimiting:Global:Enabled"] = "true",
                    ["RateLimiting:Global:PermitLimit"] = "1",
                    ["RateLimiting:Global:WindowSeconds"] = "60",
                    ["RateLimiting:Global:QueueLimit"] = "0",
                    ["ForwardedHeaders:Enabled"] = "true",
                    ["ForwardedHeaders:KnownNetworks:0"] = "0.0.0.0/0",
                    ["ForwardedHeaders:KnownNetworks:1"] = "::/0"
                });
            });
        }
    }
}
