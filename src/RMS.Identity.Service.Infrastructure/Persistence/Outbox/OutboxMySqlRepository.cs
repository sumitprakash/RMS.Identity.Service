using System.Text.Json;
using RMS.Identity.Service.Domain.Contracts.Outbox;
using RMS.Identity.Service.Domain.Interfaces.Outbox;
using RMS.Identity.Service.Infrastructure.Data;

namespace RMS.Identity.Service.Infrastructure.Persistence.Outbox;

public sealed class OutboxMySqlRepository : IOutboxRepository
{
    private readonly IMySqlConnectionFactory _connectionFactory;

    public OutboxMySqlRepository(IMySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task EnqueueAsync(
        VerificationEmailOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            userUuid = message.UserUuid,
            username = message.Username,
            displayName = message.DisplayName,
            verificationToken = message.VerificationToken,
            purpose = "email_verification"
        });

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText =
            """
            INSERT INTO Outbox (
                EventType,
                AggregateType,
                AggregateUUID,
                Payload,
                Status,
                Retries,
                CreatedAt,
                AvailableAt)
            VALUES (
                'identity.email_verification_requested',
                'UserAccount',
                UUID_TO_BIN(@UserUuid),
                CAST(@Payload AS JSON),
                'pending',
                0,
                UTC_TIMESTAMP(),
                UTC_TIMESTAMP());
            """;
        insertCommand.Parameters.AddWithValue("@UserUuid", message.UserUuid.ToString());
        insertCommand.Parameters.AddWithValue("@Payload", payload);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
