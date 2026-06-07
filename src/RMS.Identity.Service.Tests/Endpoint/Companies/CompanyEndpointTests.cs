using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySqlConnector;
using RMS.Identity.Service.Api.Endpoint.Companies;
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
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
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
