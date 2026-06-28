using System.Net;
using System.Text.Json;
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
}
