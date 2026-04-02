using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RMS.Identity.Service.Api.Endpoint.SignUp;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Entities.SignUp;

namespace RMS.Identity.Service.Tests.Endpoint.SignUp;

public sealed class SignUpEndpointTests : IClassFixture<SignUpWebApplicationFactory>
{
    private readonly SignUpWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public SignUpEndpointTests(SignUpWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_WithValidRequest_ReturnsCreatedAndMapsRequestToService()
    {
        var expectedCreatedAt = new DateTime(2026, 04, 03, 10, 15, 0, DateTimeKind.Utc);
        var expectedUser = new SignUpUser(
            Guid.Parse("7e3f8b1d-6ff6-4ec5-922e-3b5c5d9e6ef1"),
            "alice@example.com",
            "Alice Example",
            "pending",
            expectedCreatedAt);

        _factory.StubService.Handler = (command, _) => Task.FromResult(expectedUser);

        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/signup",
            new SignUpRequest
            {
                Username = "Alice@Example.com",
                Password = "StrongPass@123",
                DisplayName = "Alice Example",
                Phone = "+919876543210"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<SignUpResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal(expectedUser.UserUuid, body.UserUuid);
        Assert.Equal(expectedUser.Username, body.Username);
        Assert.Equal(expectedUser.DisplayName, body.DisplayName);
        Assert.Equal(expectedUser.Status, body.Status);
        Assert.Equal(expectedUser.CreatedAt, body.CreatedAt);

        Assert.NotNull(_factory.StubService.LastCommand);
        Assert.Equal("Alice@Example.com", _factory.StubService.LastCommand!.Username);
        Assert.Equal("StrongPass@123", _factory.StubService.LastCommand.Password);
        Assert.Equal("Alice Example", _factory.StubService.LastCommand.DisplayName);
        Assert.Equal("+919876543210", _factory.StubService.LastCommand.Phone);
        Assert.Null(_factory.StubService.LastCommand.IdempotencyKey);
    }

    [Fact]
    public async Task Post_WithIdempotencyKey_PassesHeaderValueToService()
    {
        _factory.StubService.Handler = (command, _) => Task.FromResult(new SignUpUser(
            Guid.Parse("ba64e419-4f10-4ef4-a0cf-f4dbe4c3919f"),
            "retry@example.com",
            null,
            "pending",
            new DateTime(2026, 04, 03, 10, 30, 0, DateTimeKind.Utc)));

        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/signup")
        {
            Content = JsonContent.Create(new
            {
                username = "retry@example.com",
                password = "StrongPass@123"
            })
        };
        request.Headers.Add("Idempotency-Key", "c4f78db7-6cb4-43e8-8875-51808f988ee7");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("c4f78db7-6cb4-43e8-8875-51808f988ee7", _factory.StubService.LastCommand?.IdempotencyKey);
    }

    [Fact]
    public async Task Post_WithInvalidBody_ReturnsBadRequestValidationError()
    {
        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/signup",
            new
            {
                username = "not-an-email",
                password = "short"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorContract>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal("VALIDATION_ERROR", body.Code);
        Assert.Equal("Request validation failed.", body.Message);
    }

    [Fact]
    public async Task Post_WithUnexpectedProperty_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        using var content = new StringContent(
            """
            {
              "username": "alice@example.com",
              "password": "StrongPass@123",
              "unexpected": "value"
            }
            """,
            Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync("/api/v1/signup", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WhenServiceThrowsConflict_ReturnsConflictErrorResponse()
    {
        _factory.StubService.Handler = (_, _) =>
            throw new ServiceException((int)HttpStatusCode.Conflict, "USER_EXISTS", "Username already exists.");

        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/signup",
            new
            {
                username = "alice@example.com",
                password = "StrongPass@123"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorContract>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal("USER_EXISTS", body.Code);
        Assert.Equal("Username already exists.", body.Message);
    }

    private sealed record ApiErrorContract(string Code, string Message);
}
