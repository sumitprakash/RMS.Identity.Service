using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySqlConnector;
using RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;
using RMS.Identity.Service.Api.Endpoint.Companies.GetCompany;
using RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;
using RMS.Identity.Service.Tests.Shared;

namespace RMS.Identity.Service.Tests.Endpoint.Companies;

public sealed class CompanyEndpointTests : IClassFixture<TestDatabaseWebApplicationFactory>
{
    private const string JwtIssuer = "RMS.Identity.Service";
    private const string JwtAudience = "RMS";
    private const string JwtSigningKey = "replace-this-development-signing-key-with-a-secure-secret";

    private readonly TestDatabaseWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public CompanyEndpointTests(TestDatabaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCompany_WithOwnerUser_CreatesCompanyOwnerMembershipAndCurrentUserCompany()
    {
        await _factory.EnsureCompanySchemaAsync();

        var ownerUserUuid = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var gstin = CreateUniqueGstin();
        Guid? companyUuid = null;

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var ownerUserId = await InsertUserAsync(connection, ownerUserUuid, $"owner.{Guid.NewGuid():N}@example.com", "Owner User");

        try
        {
            using var client = CreateAuthorizedClient(ownerUserUuid);
            using var request = CreatePostRequest("/api/v1/companies", new RegisterCompanyRequestBody
            {
                LegalName = "Example Retail Pvt Ltd",
                TradeName = "Example Retail",
                Gstin = gstin,
                ContactEmailAddress = "accounts@example.com",
                ContactPhoneNumber = "+919876543211",
                AddressLine1 = "1 Main Road",
                City = "Bengaluru",
                State = "Karnataka",
                PostalCode = "560001",
                Country = "IN"
            }, idempotencyKey);

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<RegisterCompanyResponse>(_jsonOptions);
            Assert.NotNull(body);
            companyUuid = body.CompanyUuid;
            Assert.Equal(gstin, body.Gstin);
            Assert.Equal("pending_verification", body.Status);

            var membership = await LoadMembershipAsync(connection, ownerUserId, companyUuid.Value);
            Assert.NotNull(membership);
            Assert.Equal("OWNER", membership.CompanyRole);
            Assert.Equal("active", membership.MembershipStatus);

            using var listResponse = await client.GetAsync("/api/v1/current-user/companies");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            var listJson = await listResponse.Content.ReadAsStringAsync();
            Assert.Contains(companyUuid.Value.ToString(), listJson);
            Assert.Contains("\"companyRole\":\"OWNER\"", listJson);
        }
        finally
        {
            if (companyUuid is not null)
            {
                await CleanupCompanyAsync(connection, companyUuid.Value);
            }

            await CleanupUserAsync(connection, ownerUserUuid);
            await CleanupIdempotencyAsync(connection, idempotencyKey);
        }
    }

    [Fact]
    public async Task PostCompany_WithDuplicateGstin_ReturnsConflict()
    {
        await _factory.EnsureCompanySchemaAsync();

        var ownerUserUuid = Guid.NewGuid();
        var existingCompanyUuid = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var gstin = CreateUniqueGstin();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        await InsertUserAsync(connection, ownerUserUuid, $"owner.{Guid.NewGuid():N}@example.com", "Owner User");
        await InsertCompanyAsync(connection, existingCompanyUuid, gstin);

        try
        {
            using var client = CreateAuthorizedClient(ownerUserUuid);
            using var request = CreatePostRequest("/api/v1/companies", new RegisterCompanyRequestBody
            {
                LegalName = "Duplicate Retail Pvt Ltd",
                Gstin = gstin,
                ContactEmailAddress = "accounts@example.com",
                ContactPhoneNumber = "+919876543211",
                AddressLine1 = "1 Main Road",
                City = "Bengaluru",
                State = "Karnataka",
                PostalCode = "560001",
                Country = "IN"
            }, idempotencyKey);

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"code\":\"COMPANY_EXISTS\"", json);
        }
        finally
        {
            await CleanupCompanyAsync(connection, existingCompanyUuid);
            await CleanupUserAsync(connection, ownerUserUuid);
            await CleanupIdempotencyAsync(connection, idempotencyKey);
        }
    }

    [Fact]
    public async Task GetCompany_WithActiveMember_ReturnsCompanyMetadata()
    {
        await _factory.EnsureCompanySchemaAsync();

        var ownerUserUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var gstin = CreateUniqueGstin();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var ownerUserId = await InsertUserAsync(connection, ownerUserUuid, $"owner.{Guid.NewGuid():N}@example.com", "Owner User");
        var companyId = await InsertCompanyAsync(connection, companyUuid, gstin);
        await InsertMembershipAsync(connection, companyId, ownerUserId, "OWNER");

        try
        {
            using var client = CreateAuthorizedClient(ownerUserUuid);

            using var response = await client.GetAsync($"/api/v1/companies/{companyUuid}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<CompanyResponse>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Equal(companyUuid, body.CompanyUuid);
            Assert.Null(body.CompanyCode);
            Assert.Equal("Existing Retail Pvt Ltd", body.LegalName);
            Assert.Equal("Existing Retail", body.TradeName);
            Assert.Equal(gstin, body.Gstin);
            Assert.Equal("accounts@example.com", body.ContactEmailAddress);
            Assert.Equal("+919876543211", body.ContactPhoneNumber);
            Assert.Equal("1 Main Road", body.RegisteredAddress.AddressLine1);
            Assert.Null(body.RegisteredAddress.AddressLine2);
            Assert.Equal("Bengaluru", body.RegisteredAddress.City);
            Assert.Equal("Karnataka", body.RegisteredAddress.State);
            Assert.Equal("560001", body.RegisteredAddress.PostalCode);
            Assert.Equal("IN", body.RegisteredAddress.Country);
            Assert.Equal("pending_verification", body.Status);
        }
        finally
        {
            await CleanupCompanyAsync(connection, companyUuid);
            await CleanupUserAsync(connection, ownerUserUuid);
        }
    }

    [Fact]
    public async Task GetCompany_WithNonMember_ReturnsForbidden()
    {
        await _factory.EnsureCompanySchemaAsync();

        var ownerUserUuid = Guid.NewGuid();
        var outsiderUserUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var ownerUserId = await InsertUserAsync(connection, ownerUserUuid, $"owner.{Guid.NewGuid():N}@example.com", "Owner User");
        await InsertUserAsync(connection, outsiderUserUuid, $"outsider.{Guid.NewGuid():N}@example.com", "Outside User");
        var companyId = await InsertCompanyAsync(connection, companyUuid, CreateUniqueGstin());
        await InsertMembershipAsync(connection, companyId, ownerUserId, "OWNER");

        try
        {
            using var client = CreateAuthorizedClient(outsiderUserUuid);

            using var response = await client.GetAsync($"/api/v1/companies/{companyUuid}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"code\":\"COMPANY_ACCESS_DENIED\"", json);
        }
        finally
        {
            await CleanupCompanyAsync(connection, companyUuid);
            await CleanupUserAsync(connection, ownerUserUuid);
            await CleanupUserAsync(connection, outsiderUserUuid);
        }
    }

    [Fact]
    public async Task GetCompany_WithUnknownCompany_ReturnsNotFound()
    {
        await _factory.EnsureCompanySchemaAsync();

        var userUuid = Guid.NewGuid();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        await InsertUserAsync(connection, userUuid, $"owner.{Guid.NewGuid():N}@example.com", "Owner User");

        try
        {
            using var client = CreateAuthorizedClient(userUuid);

            using var response = await client.GetAsync($"/api/v1/companies/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"code\":\"COMPANY_NOT_FOUND\"", json);
        }
        finally
        {
            await CleanupUserAsync(connection, userUuid);
        }
    }

    [Fact]
    public async Task GetCompanyUser_WithOwnerRole_ReturnsCompanyScopedUser()
    {
        await _factory.EnsureCompanySchemaAsync();

        var ownerUserUuid = Guid.NewGuid();
        var memberUserUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var ownerUserId = await InsertUserAsync(connection, ownerUserUuid, $"owner.{Guid.NewGuid():N}@example.com", "Owner User");
        var memberUsername = $"member.{Guid.NewGuid():N}@example.com";
        var memberUserId = await InsertUserAsync(connection, memberUserUuid, memberUsername, "Store Member");
        var companyId = await InsertCompanyAsync(connection, companyUuid, CreateUniqueGstin());
        await InsertMembershipAsync(connection, companyId, ownerUserId, "OWNER");
        await InsertMembershipAsync(connection, companyId, memberUserId, "MEMBER");

        try
        {
            using var client = CreateAuthorizedClient(ownerUserUuid);

            using var response = await client.GetAsync($"/api/v1/companies/{companyUuid}/users/{memberUserUuid}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<RMS.Identity.Service.Api.Endpoint.Companies.GetCompanyUser.UserResponse>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Equal(memberUserUuid, body.UserUuid);
            Assert.Equal(memberUsername, body.Username);
            Assert.Equal("Store Member", body.DisplayName);
            Assert.Empty(body.Roles);
            Assert.Equal("MEMBER", body.CompanyRole);
            Assert.Equal("active", body.Status);
        }
        finally
        {
            await CleanupCompanyAsync(connection, companyUuid);
            await CleanupUserAsync(connection, ownerUserUuid);
            await CleanupUserAsync(connection, memberUserUuid);
        }
    }

    [Fact]
    public async Task GetCompanyUser_WithSelfMember_ReturnsCompanyScopedUser()
    {
        await _factory.EnsureCompanySchemaAsync();

        var memberUserUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var memberUsername = $"member.{Guid.NewGuid():N}@example.com";
        var memberUserId = await InsertUserAsync(connection, memberUserUuid, memberUsername, "Store Member");
        var companyId = await InsertCompanyAsync(connection, companyUuid, CreateUniqueGstin());
        await InsertMembershipAsync(connection, companyId, memberUserId, "MEMBER");

        try
        {
            using var client = CreateAuthorizedClient(memberUserUuid);

            using var response = await client.GetAsync($"/api/v1/companies/{companyUuid}/users/{memberUserUuid}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<RMS.Identity.Service.Api.Endpoint.Companies.GetCompanyUser.UserResponse>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Equal(memberUserUuid, body.UserUuid);
            Assert.Equal(memberUsername, body.Username);
            Assert.Equal("MEMBER", body.CompanyRole);
            Assert.Equal("active", body.Status);
        }
        finally
        {
            await CleanupCompanyAsync(connection, companyUuid);
            await CleanupUserAsync(connection, memberUserUuid);
        }
    }

    [Fact]
    public async Task GetCompanyUser_WithMemberViewingOtherUser_ReturnsForbidden()
    {
        await _factory.EnsureCompanySchemaAsync();

        var actorUserUuid = Guid.NewGuid();
        var targetUserUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var actorUserId = await InsertUserAsync(connection, actorUserUuid, $"member.{Guid.NewGuid():N}@example.com", "Actor Member");
        var targetUserId = await InsertUserAsync(connection, targetUserUuid, $"target.{Guid.NewGuid():N}@example.com", "Target Member");
        var companyId = await InsertCompanyAsync(connection, companyUuid, CreateUniqueGstin());
        await InsertMembershipAsync(connection, companyId, actorUserId, "MEMBER");
        await InsertMembershipAsync(connection, companyId, targetUserId, "MEMBER");

        try
        {
            using var client = CreateAuthorizedClient(actorUserUuid);

            using var response = await client.GetAsync($"/api/v1/companies/{companyUuid}/users/{targetUserUuid}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"code\":\"COMPANY_ROLE_REQUIRED\"", json);
        }
        finally
        {
            await CleanupCompanyAsync(connection, companyUuid);
            await CleanupUserAsync(connection, actorUserUuid);
            await CleanupUserAsync(connection, targetUserUuid);
        }
    }

    [Fact]
    public async Task GetCompanyUsers_WithOwnerRole_ReturnsCompanyUsers()
    {
        await _factory.EnsureCompanySchemaAsync();

        var ownerUserUuid = Guid.NewGuid();
        var memberUserUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var ownerUsername = $"owner.{Guid.NewGuid():N}@example.com";
        var memberUsername = $"member.{Guid.NewGuid():N}@example.com";
        var ownerUserId = await InsertUserAsync(connection, ownerUserUuid, ownerUsername, "Owner User");
        var memberUserId = await InsertUserAsync(connection, memberUserUuid, memberUsername, "Store Member");
        var companyId = await InsertCompanyAsync(connection, companyUuid, CreateUniqueGstin());
        await InsertMembershipAsync(connection, companyId, ownerUserId, "OWNER");
        await InsertMembershipAsync(connection, companyId, memberUserId, "MEMBER");

        try
        {
            using var client = CreateAuthorizedClient(ownerUserUuid);

            using var response = await client.GetAsync($"/api/v1/companies/{companyUuid}/users");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<RMS.Identity.Service.Api.Endpoint.Companies.ListCompanyUsers.ListCompanyUsersResponse>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Contains(body.Users, user => user.UserUuid == ownerUserUuid && user.CompanyRole == "OWNER");
            Assert.Contains(body.Users, user => user.UserUuid == memberUserUuid && user.CompanyRole == "MEMBER");
        }
        finally
        {
            await CleanupCompanyAsync(connection, companyUuid);
            await CleanupUserAsync(connection, ownerUserUuid);
            await CleanupUserAsync(connection, memberUserUuid);
        }
    }

    [Fact]
    public async Task PatchCompanyUser_WithOwnerRole_UpdatesMembership()
    {
        await _factory.EnsureCompanySchemaAsync();

        var ownerUserUuid = Guid.NewGuid();
        var memberUserUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var ownerUserId = await InsertUserAsync(connection, ownerUserUuid, $"owner.{Guid.NewGuid():N}@example.com", "Owner User");
        var memberUserId = await InsertUserAsync(connection, memberUserUuid, $"member.{Guid.NewGuid():N}@example.com", "Store Member");
        var companyId = await InsertCompanyAsync(connection, companyUuid, CreateUniqueGstin());
        await InsertMembershipAsync(connection, companyId, ownerUserId, "OWNER");
        await InsertMembershipAsync(connection, companyId, memberUserId, "MEMBER");

        try
        {
            using var client = CreateAuthorizedClient(ownerUserUuid);
            using var request = CreateJsonRequest(
                HttpMethod.Patch,
                $"/api/v1/companies/{companyUuid}/users/{memberUserUuid}",
                new RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompanyUser.UpdateCompanyUserRequestBody
                {
                    CompanyRole = "ADMIN",
                    MembershipStatus = "active"
                },
                idempotencyKey);

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompanyUser.UserResponse>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Equal("ADMIN", body.CompanyRole);
            Assert.Equal("active", body.Status);

            var membership = await LoadMembershipAsync(connection, memberUserId, companyUuid);
            Assert.NotNull(membership);
            Assert.Equal("ADMIN", membership.CompanyRole);
            Assert.Equal("active", membership.MembershipStatus);
        }
        finally
        {
            await CleanupCompanyAsync(connection, companyUuid);
            await CleanupUserAsync(connection, ownerUserUuid);
            await CleanupUserAsync(connection, memberUserUuid);
            await CleanupIdempotencyAsync(connection, idempotencyKey);
        }
    }

    [Fact]
    public async Task DeleteCompanyUser_WithOwnerRole_SuspendsMembership()
    {
        await _factory.EnsureCompanySchemaAsync();

        var ownerUserUuid = Guid.NewGuid();
        var memberUserUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var ownerUserId = await InsertUserAsync(connection, ownerUserUuid, $"owner.{Guid.NewGuid():N}@example.com", "Owner User");
        var memberUserId = await InsertUserAsync(connection, memberUserUuid, $"member.{Guid.NewGuid():N}@example.com", "Store Member");
        var companyId = await InsertCompanyAsync(connection, companyUuid, CreateUniqueGstin());
        await InsertMembershipAsync(connection, companyId, ownerUserId, "OWNER");
        await InsertMembershipAsync(connection, companyId, memberUserId, "MEMBER");

        try
        {
            using var client = CreateAuthorizedClient(ownerUserUuid);
            using var request = CreateJsonRequest(
                HttpMethod.Delete,
                $"/api/v1/companies/{companyUuid}/users/{memberUserUuid}",
                body: null,
                idempotencyKey);

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var membership = await LoadMembershipAsync(connection, memberUserId, companyUuid);
            Assert.NotNull(membership);
            Assert.Equal("MEMBER", membership.CompanyRole);
            Assert.Equal("suspended", membership.MembershipStatus);
        }
        finally
        {
            await CleanupCompanyAsync(connection, companyUuid);
            await CleanupUserAsync(connection, ownerUserUuid);
            await CleanupUserAsync(connection, memberUserUuid);
            await CleanupIdempotencyAsync(connection, idempotencyKey);
        }
    }

    [Fact]
    public async Task PatchCompany_WithOwnerRole_UpdatesCompanyMetadata()
    {
        await _factory.EnsureCompanySchemaAsync();

        var ownerUserUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var updatedGstin = CreateUniqueGstin();

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var ownerUserId = await InsertUserAsync(connection, ownerUserUuid, $"owner.{Guid.NewGuid():N}@example.com", "Owner User");
        var companyId = await InsertCompanyAsync(connection, companyUuid, CreateUniqueGstin());
        await InsertMembershipAsync(connection, companyId, ownerUserId, "OWNER");

        try
        {
            using var client = CreateAuthorizedClient(ownerUserUuid);
            using var request = CreateJsonRequest(
                HttpMethod.Patch,
                $"/api/v1/companies/{companyUuid}",
                new RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompany.UpdateCompanyRequestBody
                {
                    LegalName = "Updated Retail Pvt Ltd",
                    TradeName = "Updated Retail",
                    Gstin = updatedGstin,
                    ContactEmailAddress = "billing@example.com",
                    ContactPhoneNumber = "+919876543211",
                    AddressLine1 = "2 Main Road",
                    AddressLine2 = "Near Market",
                    City = "Bengaluru",
                    State = "Karnataka",
                    PostalCode = "560002",
                    Country = "IN"
                },
                idempotencyKey);

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompany.CompanyResponse>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Equal("Updated Retail Pvt Ltd", body.LegalName);
            Assert.Equal("Updated Retail", body.TradeName);
            Assert.Equal(updatedGstin, body.Gstin);
            Assert.Equal("billing@example.com", body.ContactEmailAddress);
            Assert.Equal("2 Main Road", body.RegisteredAddress.AddressLine1);
            Assert.Equal("Near Market", body.RegisteredAddress.AddressLine2);
            Assert.Equal("pending_verification", body.Status);
        }
        finally
        {
            await CleanupCompanyAsync(connection, companyUuid);
            await CleanupUserAsync(connection, ownerUserUuid);
            await CleanupIdempotencyAsync(connection, idempotencyKey);
        }
    }

    [Fact]
    public async Task PostCompanyUser_WithOwnerRole_CreatesGlobalUserAndCompanyMembership()
    {
        await _factory.EnsureCompanySchemaAsync();

        var ownerUserUuid = Guid.NewGuid();
        var companyUuid = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var newUsername = $"cashier.{Guid.NewGuid():N}@example.com";
        Guid? createdUserUuid = null;

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        var ownerUserId = await InsertUserAsync(connection, ownerUserUuid, $"owner.{Guid.NewGuid():N}@example.com", "Owner User");
        var companyId = await InsertCompanyAsync(connection, companyUuid, CreateUniqueGstin());
        await InsertMembershipAsync(connection, companyId, ownerUserId, "OWNER");

        try
        {
            using var client = CreateAuthorizedClient(ownerUserUuid);
            using var request = CreatePostRequest($"/api/v1/companies/{companyUuid}/users", new CreateCompanyUserRequestBody
            {
                Username = newUsername,
                DisplayName = "Store Cashier",
                CompanyRole = "MEMBER"
            }, idempotencyKey);

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<UserResponse>(_jsonOptions);
            Assert.NotNull(body);
            createdUserUuid = body.UserUuid;
            Assert.Equal(newUsername, body.Username);
            Assert.Equal("Store Cashier", body.DisplayName);
            Assert.Equal("MEMBER", body.CompanyRole);
            Assert.Equal("pending", body.Status);

            var createdUserId = await LoadUserIdAsync(connection, createdUserUuid.Value);
            var membership = await LoadMembershipAsync(connection, createdUserId, companyUuid);
            Assert.NotNull(membership);
            Assert.Equal("MEMBER", membership.CompanyRole);
            Assert.Equal("active", membership.MembershipStatus);
        }
        finally
        {
            if (createdUserUuid is not null)
            {
                await CleanupUserAsync(connection, createdUserUuid.Value);
            }

            await CleanupCompanyAsync(connection, companyUuid);
            await CleanupUserAsync(connection, ownerUserUuid);
            await CleanupIdempotencyAsync(connection, idempotencyKey);
        }
    }

    private HttpClient CreateAuthorizedClient(Guid userUuid)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userUuid));
        return client;
    }

    private static HttpRequestMessage CreatePostRequest(string path, object body, string idempotencyKey)
    {
        return CreateJsonRequest(HttpMethod.Post, path, body, idempotencyKey);
    }

    private static HttpRequestMessage CreateJsonRequest(
        HttpMethod method,
        string path,
        object? body,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private static string CreateJwt(Guid userUuid)
    {
        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = JwtIssuer,
            aud = JwtAudience,
            sub = userUuid.ToString(),
            exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds()
        }));
        var unsignedToken = $"{header}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(JwtSigningKey));
        return $"{unsignedToken}.{Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken)))}";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string CreateUniqueGstin()
    {
        var number = RandomNumberGenerator.GetInt32(1000, 9999);
        return $"29ABCDE{number}F1Z5";
    }

    private static async Task<long> InsertUserAsync(
        MySqlConnection connection,
        Guid userUuid,
        string username,
        string displayName)
    {
        await ExecuteNonQueryAsync(
            connection,
            """
            INSERT INTO UserAccount (UserUUID, Username, PasswordHash, DisplayName, EmailVerified, IsActive, IsDeleted, CreatedAt)
            VALUES (UUID_TO_BIN(@UserUuid), @Username, '$2a$11$testtesttesttesttesttesttesttesttesttesttesttesttestte', @DisplayName, 1, 1, 0, UTC_TIMESTAMP());
            """,
            command =>
            {
                command.Parameters.AddWithValue("@UserUuid", userUuid.ToString());
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@DisplayName", displayName);
            });

        return await LoadUserIdAsync(connection, userUuid);
    }

    private static async Task<long> InsertCompanyAsync(
        MySqlConnection connection,
        Guid companyUuid,
        string gstin)
    {
        await ExecuteNonQueryAsync(
            connection,
            """
            INSERT INTO Company (
                CompanyUUID, LegalName, TradeName, CompanyGSTIN, ContactEmailAddress, ContactPhoneNumber,
                AddressLine1, City, State, PostalCode, Country, CompanyStatus, IsDeleted, CreatedAt)
            VALUES (
                UUID_TO_BIN(@CompanyUuid), 'Existing Retail Pvt Ltd', 'Existing Retail', @Gstin, 'accounts@example.com', '+919876543211',
                '1 Main Road', 'Bengaluru', 'Karnataka', '560001', 'IN', 'pending_verification', 0, UTC_TIMESTAMP());
            """,
            command =>
            {
                command.Parameters.AddWithValue("@CompanyUuid", companyUuid.ToString());
                command.Parameters.AddWithValue("@Gstin", gstin);
            });

        return await LoadCompanyIdAsync(connection, companyUuid);
    }

    private static async Task InsertMembershipAsync(
        MySqlConnection connection,
        long companyId,
        long userId,
        string companyRole)
    {
        await ExecuteNonQueryAsync(
            connection,
            """
            INSERT INTO CompanyUser (CompanyID, UserID, CompanyRole, MembershipStatus, JoinedAt, CreatedAt)
            VALUES (@CompanyId, @UserId, @CompanyRole, 'active', UTC_TIMESTAMP(), UTC_TIMESTAMP());
            """,
            command =>
            {
                command.Parameters.AddWithValue("@CompanyId", companyId);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CompanyRole", companyRole);
            });
    }

    private static async Task<long> LoadUserIdAsync(MySqlConnection connection, Guid userUuid) =>
        Convert.ToInt64(await ExecuteScalarAsync(
            connection,
            "SELECT UserID FROM UserAccount WHERE UserUUID = UUID_TO_BIN(@UserUuid);",
            command => command.Parameters.AddWithValue("@UserUuid", userUuid.ToString())));

    private static async Task<long> LoadCompanyIdAsync(MySqlConnection connection, Guid companyUuid) =>
        Convert.ToInt64(await ExecuteScalarAsync(
            connection,
            "SELECT CompanyID FROM Company WHERE CompanyUUID = UUID_TO_BIN(@CompanyUuid);",
            command => command.Parameters.AddWithValue("@CompanyUuid", companyUuid.ToString())));

    private static async Task<CompanyMembershipRecord?> LoadMembershipAsync(
        MySqlConnection connection,
        long userId,
        Guid companyUuid)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT cu.CompanyRole, cu.MembershipStatus
            FROM CompanyUser cu
            INNER JOIN Company c ON c.CompanyID = cu.CompanyID
            WHERE cu.UserID = @UserId
              AND c.CompanyUUID = UUID_TO_BIN(@CompanyUuid)
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@CompanyUuid", companyUuid.ToString());

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new CompanyMembershipRecord(
            reader.GetString(reader.GetOrdinal("CompanyRole")),
            reader.GetString(reader.GetOrdinal("MembershipStatus")));
    }

    private static async Task CleanupCompanyAsync(MySqlConnection connection, Guid companyUuid)
    {
        await ExecuteNonQueryAsync(
            connection,
            """
            DELETE cu
            FROM CompanyUser cu
            INNER JOIN Company c ON c.CompanyID = cu.CompanyID
            WHERE c.CompanyUUID = UUID_TO_BIN(@CompanyUuid);
            """,
            command => command.Parameters.AddWithValue("@CompanyUuid", companyUuid.ToString()));

        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM Company WHERE CompanyUUID = UUID_TO_BIN(@CompanyUuid);",
            command => command.Parameters.AddWithValue("@CompanyUuid", companyUuid.ToString()));
    }

    private static async Task CleanupUserAsync(MySqlConnection connection, Guid userUuid)
    {
        await ExecuteNonQueryAsync(
            connection,
            """
            DELETE cu
            FROM CompanyUser cu
            INNER JOIN UserAccount ua ON ua.UserID = cu.UserID
            WHERE ua.UserUUID = UUID_TO_BIN(@UserUuid);
            """,
            command => command.Parameters.AddWithValue("@UserUuid", userUuid.ToString()));

        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM UserAccount WHERE UserUUID = UUID_TO_BIN(@UserUuid);",
            command => command.Parameters.AddWithValue("@UserUuid", userUuid.ToString()));
    }

    private static Task CleanupIdempotencyAsync(MySqlConnection connection, string key) =>
        ExecuteNonQueryAsync(
            connection,
            "DELETE FROM IdempotencyKey WHERE KeyValue = @KeyValue;",
            command => command.Parameters.AddWithValue("@KeyValue", key));

    private static async Task ExecuteNonQueryAsync(
        MySqlConnection connection,
        string commandText,
        Action<MySqlCommand> configure)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        configure(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object> ExecuteScalarAsync(
        MySqlConnection connection,
        string commandText,
        Action<MySqlCommand> configure)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        configure(command);
        return await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Expected scalar query to return a value.");
    }

    private sealed record CompanyMembershipRecord(string CompanyRole, string MembershipStatus);
}
