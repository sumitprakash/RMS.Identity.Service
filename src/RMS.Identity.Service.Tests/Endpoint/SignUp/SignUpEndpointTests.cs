using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySqlConnector;
using RMS.Identity.Service.Api.Endpoint.SignUp;
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
    public async Task Post_WithValidRequest_ReturnsCreatedAndPersistsSignupData()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var username = $"Alice.{uniqueSuffix}@Example.com";
        var normalizedUsername = username.ToLowerInvariant();
        var password = "StrongPass@123";
        var idempotencyKey = Guid.NewGuid().ToString();
        var beforeRequest = DateTime.UtcNow;
        SignUpResponse? body = null;

        try
        {
            using var client = _factory.CreateClient();
            using var request = CreateSignUpRequest(new SignUpRequestBody
            {
                EmailAddress = username,
                Password = password,
                FirstName = " Alice ",
                LastName = " Example ",
                PhoneNumber = "+919876543210"
            }, idempotencyKey);
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            body = await response.Content.ReadFromJsonAsync<SignUpResponse>(_jsonOptions);
            Assert.NotNull(body);
            Assert.NotEqual(Guid.Empty, body.UserUuid);
            Assert.Equal(normalizedUsername, body.EmailAddress);
            Assert.Equal("pending", body.Status);
            Assert.True(body.CreatedAt >= beforeRequest.AddSeconds(-5));
            Assert.True(body.CreatedAt <= DateTime.UtcNow.AddSeconds(5));

            var persisted = await LoadPersistedSignUpAsync(normalizedUsername, body.UserUuid);
            Assert.NotNull(persisted);
            Assert.Equal(body.UserUuid, persisted.UserUuid);
            Assert.Equal(normalizedUsername, persisted.Username);
            Assert.Equal("Alice Example", persisted.DisplayName);
            Assert.NotEqual(password, persisted.PasswordHash);
            Assert.StartsWith("$2", persisted.PasswordHash);
            Assert.False(persisted.EmailVerified);
            Assert.True(persisted.IsActive);
            Assert.False(persisted.IsDeleted);
            Assert.Equal(body.CreatedAt, persisted.CreatedAt);
            Assert.Equal(1, persisted.EmailVerificationCount);
            Assert.False(persisted.EmailVerificationConsumed);
            Assert.True(persisted.EmailVerificationExpiresAt > DateTime.UtcNow);
            Assert.Equal(1, persisted.AuditLogCount);
            Assert.Equal(1, persisted.OutboxCount);
        }
        finally
        {
            if (body is not null)
            {
                await CleanupSignUpDataAsync(normalizedUsername, body.UserUuid, idempotencyKey);
            }
        }
    }

    [Fact]
    public async Task Post_WithIdempotencyKey_ReturnsCreatedAndStoresIdempotentDatabaseEntry()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var normalizedUsername = $"retry.{uniqueSuffix}@example.com";
        var password = "StrongPass@123";
        var idempotencyKey = Guid.NewGuid().ToString();
        SignUpResponse? body = null;

        try
        {
            using var client = _factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/signup")
            {
                Content = JsonContent.Create(new
                {
                    emailAddress = normalizedUsername,
                    password,
                    firstName = "Retry",
                    middleName = (string?)null,
                    lastName = "Example",
                    phoneNumber = "+919876543210"
                })
            };
            request.Headers.Add("Idempotency-Key", idempotencyKey);

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            body = await response.Content.ReadFromJsonAsync<SignUpResponse>(_jsonOptions);
            Assert.NotNull(body);

            var idempotencyEntry = await LoadIdempotencyEntryAsync(idempotencyKey);
            Assert.NotNull(idempotencyEntry);
            Assert.Equal("POST", idempotencyEntry.Method);
            Assert.Equal("/api/v1/signup", idempotencyEntry.Route);
            Assert.False(string.IsNullOrWhiteSpace(idempotencyEntry.RequestHash));
            Assert.Equal(ComputeSha256Hex(JsonSerializer.Serialize(new
            {
                emailAddress = normalizedUsername,
                firstName = "Retry",
                middleName = (string?)null,
                lastName = "Example",
                phoneNumber = "+919876543210"
            })), idempotencyEntry.RequestHash);
            Assert.NotEqual(ComputeSha256Hex(JsonSerializer.Serialize(new
            {
                emailAddress = normalizedUsername,
                password,
                firstName = "Retry",
                middleName = (string?)null,
                lastName = "Example",
                phoneNumber = "+919876543210"
            })), idempotencyEntry.RequestHash);
            Assert.Equal(201, idempotencyEntry.ResponseCode);
            Assert.Contains(body.UserUuid.ToString(), idempotencyEntry.ResponseBody);
        }
        finally
        {
            if (body is not null)
            {
                await CleanupSignUpDataAsync(normalizedUsername, body.UserUuid, idempotencyKey);
            }
        }
    }

    [Fact]
    public async Task Post_WhenIdempotencyReservationCollidesWithCommittedResponse_ReturnsStoredResponse()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var normalizedUsername = $"race.{uniqueSuffix}@example.com";
        var password = "StrongPass@123";
        var idempotencyKey = Guid.NewGuid().ToString();
        var storedUser = new SignUpUser(
            Guid.NewGuid(),
            normalizedUsername,
            null,
            "pending",
            DateTime.SpecifyKind(DateTime.UtcNow.AddSeconds(-1), DateTimeKind.Utc));
        var requestHash = ComputeSha256Hex(JsonSerializer.Serialize(new
        {
            emailAddress = normalizedUsername,
            firstName = "Race",
            middleName = (string?)null,
            lastName = "Example",
            phoneNumber = "+919876543210"
        }));
        var committed = false;

        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            await ExecuteNonQueryAsync(
                connection,
                """
                INSERT INTO IdempotencyKey (KeyValue, Method, Route, RequestHash)
                VALUES (@KeyValue, 'POST', '/api/v1/signup', @RequestHash);
                """,
                command =>
                {
                    command.Parameters.AddWithValue("@KeyValue", idempotencyKey);
                    command.Parameters.AddWithValue("@RequestHash", requestHash);
                },
                transaction);

            using var client = _factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/signup")
            {
                Content = JsonContent.Create(new
                {
                    emailAddress = normalizedUsername,
                    password,
                    firstName = "Race",
                    middleName = (string?)null,
                    lastName = "Example",
                    phoneNumber = "+919876543210"
                })
            };
            request.Headers.Add("Idempotency-Key", idempotencyKey);

            var blockedResponseTask = client.SendAsync(request);
            await WaitForBlockedIdempotencyInsertAsync(connection, transaction, blockedResponseTask);

            await ExecuteNonQueryAsync(
                connection,
                """
                UPDATE IdempotencyKey
                SET ResponseCode = 201,
                    ResponseBody = CAST(@ResponseBody AS JSON)
                WHERE KeyValue = @KeyValue;
                """,
                command =>
                {
                    command.Parameters.AddWithValue("@KeyValue", idempotencyKey);
                    command.Parameters.AddWithValue("@ResponseBody", JsonSerializer.Serialize(storedUser));
                },
                transaction);

            await transaction.CommitAsync();
            committed = true;

            using var response = await blockedResponseTask.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<SignUpResponse>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Equal(storedUser.UserUuid, body.UserUuid);
            Assert.Equal(storedUser.Username, body.EmailAddress);
            Assert.Equal(storedUser.Status, body.Status);
        }
        finally
        {
            if (!committed)
            {
                await transaction.RollbackAsync();
            }

            await ExecuteNonQueryAsync(
                connection,
                "DELETE FROM IdempotencyKey WHERE KeyValue = @KeyValue;",
                command => command.Parameters.AddWithValue("@KeyValue", idempotencyKey));
        }
    }

    [Fact]
    public async Task Post_WhenOutboxInsertFails_ReturnsCreatedAndKeepsSignupData()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var normalizedUsername = $"outbox-failure.{uniqueSuffix}@example.com";
        var constraintName = $"chk_signup_outbox_failure_{uniqueSuffix[..16]}";
        var idempotencyKey = Guid.NewGuid().ToString();
        SignUpResponse? body = null;

        await using var connection = await _factory.OpenDatabaseConnectionAsync();

        try
        {
            await CreateOutboxFailureCheckConstraintAsync(connection, constraintName, normalizedUsername);

            using var client = _factory.CreateClient();
            using var request = CreateSignUpRequest(CreateValidBody(normalizedUsername), idempotencyKey);
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            body = await response.Content.ReadFromJsonAsync<SignUpResponse>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Equal(normalizedUsername, body.EmailAddress);

            var persisted = await LoadPersistedSignUpAsync(normalizedUsername, body.UserUuid);
            Assert.NotNull(persisted);
            Assert.Equal(body.UserUuid, persisted.UserUuid);
            Assert.Equal(normalizedUsername, persisted.Username);
            Assert.Equal(1, persisted.EmailVerificationCount);
            Assert.Equal(1, persisted.AuditLogCount);
            Assert.Equal(0, persisted.OutboxCount);
        }
        finally
        {
            await DropCheckConstraintAsync(connection, constraintName);

            if (body is not null)
            {
                await CleanupSignUpDataAsync(normalizedUsername, body.UserUuid, idempotencyKey);
            }
        }
    }

    [Fact]
    public async Task Post_WithInvalidBody_ReturnsBadRequestValidationError()
    {
        using var client = _factory.CreateClient();
        using var request = CreateSignUpRequest(new
        {
            emailAddress = "not-an-email",
            password = "short",
            firstName = "",
            lastName = "",
            phoneNumber = ""
        });
        using var response = await client.SendAsync(request);

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
              "emailAddress": "alice@example.com",
              "password": "StrongPass@123",
              "firstName": "Alice",
              "lastName": "Example",
              "phoneNumber": "+919876543210",
              "unexpected": "value"
            }
            """,
            Encoding.UTF8,
            "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/signup")
        {
            Content = content
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutIdempotencyKey_ReturnsBadRequestValidationError()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/signup")
        {
            Content = JsonContent.Create(CreateValidBody("missing-idempotency@example.com"))
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorContract>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal("VALIDATION_ERROR", body.Code);
        Assert.Equal("Idempotency-Key is required.", body.Message);
    }

    [Fact]
    public async Task Post_WithInvalidIdempotencyKey_ReturnsBadRequestValidationError()
    {
        using var client = _factory.CreateClient();
        using var request = CreateSignUpRequest(
            CreateValidBody("invalid-idempotency@example.com"),
            "not-a-guid");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorContract>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal("VALIDATION_ERROR", body.Code);
        Assert.Equal("Idempotency-Key must be a valid UUID.", body.Message);
    }

    [Fact]
    public async Task Post_WhenUsernameAlreadyExists_ReturnsConflictErrorResponse()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var normalizedUsername = $"duplicate.{uniqueSuffix}@example.com";
        var firstIdempotencyKey = Guid.NewGuid().ToString();
        var secondIdempotencyKey = Guid.NewGuid().ToString();
        SignUpResponse? body = null;

        try
        {
            using var client = _factory.CreateClient();
            using var firstRequest = CreateSignUpRequest(CreateValidBody(normalizedUsername), firstIdempotencyKey);
            using var firstResponse = await client.SendAsync(firstRequest);
            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
            body = await firstResponse.Content.ReadFromJsonAsync<SignUpResponse>(_jsonOptions);

            using var secondRequest = CreateSignUpRequest(
                CreateValidBody(normalizedUsername, "AnotherStrongPass@123"),
                secondIdempotencyKey);
            using var response = await client.SendAsync(secondRequest);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"code\"", json);
            Assert.Contains("\"message\"", json);
            Assert.DoesNotContain("\"Code\"", json);
            Assert.DoesNotContain("\"Message\"", json);

            var error = await response.Content.ReadFromJsonAsync<ApiErrorContract>(_jsonOptions);
            Assert.NotNull(error);
            Assert.Equal("USER_EXISTS", error.Code);
            Assert.Equal("Email address already exists.", error.Message);
        }
        finally
        {
            if (body is not null)
            {
                await CleanupSignUpDataAsync(normalizedUsername, body.UserUuid, firstIdempotencyKey);
                await CleanupSignUpDataAsync(normalizedUsername, body.UserUuid, secondIdempotencyKey);
            }
        }
    }

    private static SignUpRequestBody CreateValidBody(string emailAddress, string password = "StrongPass@123") =>
        new()
        {
            EmailAddress = emailAddress,
            Password = password,
            FirstName = "Alice",
            MiddleName = null,
            LastName = "Example",
            PhoneNumber = "+919876543210"
        };

    private static HttpRequestMessage CreateSignUpRequest(object body, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/signup")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
        return request;
    }

    private async Task<PersistedSignUpUser?> LoadPersistedSignUpAsync(string username, Guid userUuid)
    {
        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                ua.UserID,
                BIN_TO_UUID(ua.UserUUID) AS UserUuid,
                ua.Username,
                ua.PasswordHash,
                ua.DisplayName,
                ua.EmailVerified,
                ua.IsActive,
                ua.IsDeleted,
                ua.CreatedAt,
                COUNT(DISTINCT ev.EmailVerificationID) AS EmailVerificationCount,
                MAX(ev.Consumed) AS EmailVerificationConsumed,
                MAX(ev.ExpiresAt) AS EmailVerificationExpiresAt,
                COUNT(DISTINCT al.AuditID) AS AuditLogCount,
                COUNT(DISTINCT ob.OutboxID) AS OutboxCount
            FROM UserAccount ua
            LEFT JOIN EmailVerification ev ON ev.UserID = ua.UserID AND ev.Purpose = 'email_verification'
            LEFT JOIN AuditLog al ON al.RecordId = BIN_TO_UUID(ua.UserUUID) AND al.Action = 'signup_created'
            LEFT JOIN Outbox ob ON ob.AggregateUUID = ua.UserUUID AND ob.EventType = 'identity.email_verification_requested'
            WHERE ua.Username = @Username
              AND ua.UserUUID = UUID_TO_BIN(@UserUuid)
            GROUP BY
                ua.UserID,
                ua.UserUUID,
                ua.Username,
                ua.PasswordHash,
                ua.DisplayName,
                ua.EmailVerified,
                ua.IsActive,
                ua.IsDeleted,
                ua.CreatedAt
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Username", username);
        command.Parameters.AddWithValue("@UserUuid", userUuid.ToString());

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new PersistedSignUpUser(
            reader.GetInt64(reader.GetOrdinal("UserID")),
            Guid.Parse(reader.GetString(reader.GetOrdinal("UserUuid"))),
            reader.GetString(reader.GetOrdinal("Username")),
            reader.GetString(reader.GetOrdinal("PasswordHash")),
            reader.IsDBNull(reader.GetOrdinal("DisplayName")) ? null : reader.GetString(reader.GetOrdinal("DisplayName")),
            reader.GetBoolean(reader.GetOrdinal("EmailVerified")),
            reader.GetBoolean(reader.GetOrdinal("IsActive")),
            reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
            DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("CreatedAt")), DateTimeKind.Utc),
            reader.GetInt32(reader.GetOrdinal("EmailVerificationCount")),
            reader.GetBoolean(reader.GetOrdinal("EmailVerificationConsumed")),
            DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("EmailVerificationExpiresAt")), DateTimeKind.Utc),
            reader.GetInt32(reader.GetOrdinal("AuditLogCount")),
            reader.GetInt32(reader.GetOrdinal("OutboxCount")));
    }

    private async Task<IdempotencyEntry?> LoadIdempotencyEntryAsync(string idempotencyKey)
    {
        await using var connection = await _factory.OpenDatabaseConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Method, Route, RequestHash, ResponseCode, CAST(ResponseBody AS CHAR) AS ResponseBody
            FROM IdempotencyKey
            WHERE KeyValue = @KeyValue
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@KeyValue", idempotencyKey);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new IdempotencyEntry(
            reader.GetString(reader.GetOrdinal("Method")),
            reader.GetString(reader.GetOrdinal("Route")),
            reader.GetString(reader.GetOrdinal("RequestHash")),
            reader.GetInt32(reader.GetOrdinal("ResponseCode")),
            reader.GetString(reader.GetOrdinal("ResponseBody")));
    }

    private async Task CleanupSignUpDataAsync(string username, Guid userUuid, string? idempotencyKey)
    {
        await using var connection = await _factory.OpenDatabaseConnectionAsync();

        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM Outbox WHERE AggregateUUID = UUID_TO_BIN(@UserUuid);",
            command => command.Parameters.AddWithValue("@UserUuid", userUuid.ToString()));

        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM AuditLog WHERE RecordId = @UserUuid;",
            command => command.Parameters.AddWithValue("@UserUuid", userUuid.ToString()));

        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM EmailVerification WHERE UserID IN (SELECT UserID FROM UserAccount WHERE Username = @Username);",
            command => command.Parameters.AddWithValue("@Username", username));

        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM UserAccount WHERE Username = @Username;",
            command => command.Parameters.AddWithValue("@Username", username));

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await ExecuteNonQueryAsync(
                connection,
                "DELETE FROM IdempotencyKey WHERE KeyValue = @KeyValue;",
                command => command.Parameters.AddWithValue("@KeyValue", idempotencyKey));
        }
    }

    private static async Task ExecuteNonQueryAsync(
        MySqlConnection connection,
        string commandText,
        Action<MySqlCommand> configure,
        MySqlTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        configure(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task WaitForBlockedIdempotencyInsertAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        Task<HttpResponseMessage> responseTask)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (!timeout.IsCancellationRequested)
        {
            if (responseTask.IsCompleted)
            {
                throw new InvalidOperationException("The idempotent request completed before reaching the reservation collision.");
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                SELECT COUNT(*)
                FROM information_schema.PROCESSLIST
                WHERE ID <> CONNECTION_ID()
                  AND INFO LIKE '%INSERT INTO IdempotencyKey%';
                """;

            var matchingProcesses = Convert.ToInt32(await command.ExecuteScalarAsync(timeout.Token));
            if (matchingProcesses > 0)
            {
                return;
            }

            await Task.Delay(50, timeout.Token);
        }

        throw new TimeoutException("Timed out waiting for the idempotent request to block on the duplicate key insert.");
    }

    private static Task CreateOutboxFailureCheckConstraintAsync(
        MySqlConnection connection,
        string constraintName,
        string username)
    {
        return ExecuteNonQueryAsync(
            connection,
            $"""
            ALTER TABLE Outbox
            ADD CONSTRAINT `{constraintName}`
            CHECK (JSON_UNQUOTE(JSON_EXTRACT(Payload, '$.username')) <> @Username);
            """,
            command => command.Parameters.AddWithValue("@Username", username));
    }

    private static Task DropCheckConstraintAsync(MySqlConnection connection, string constraintName)
    {
        return ExecuteNonQueryAsync(
            connection,
            $"ALTER TABLE Outbox DROP CHECK `{constraintName}`;",
            _ => { });
    }

    private static string ComputeSha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed record PersistedSignUpUser(
        long UserId,
        Guid UserUuid,
        string Username,
        string PasswordHash,
        string? DisplayName,
        bool EmailVerified,
        bool IsActive,
        bool IsDeleted,
        DateTime CreatedAt,
        int EmailVerificationCount,
        bool EmailVerificationConsumed,
        DateTime EmailVerificationExpiresAt,
        int AuditLogCount,
        int OutboxCount);

    private sealed record IdempotencyEntry(
        string Method,
        string Route,
        string RequestHash,
        int ResponseCode,
        string ResponseBody);

    private sealed record ApiErrorContract(string Code, string Message);
}
