using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Domain.Entities.Outbox;
using RMS.Identity.Service.Domain.Interfaces.Notifications;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;
using RMS.Identity.Service.Infrastructure.Notifications;

namespace RMS.Identity.Service.Infrastructure.Outbox;

public sealed class EmailVerificationRequestedOutboxProcessor
{
    public const string EventType = "email_verification_requested";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IOutboxProcessingRepository _outboxRepository;
    private readonly IEmailSender _emailSender;
    private readonly EmailDeliveryOptions _options;
    private readonly ILogger<EmailVerificationRequestedOutboxProcessor> _logger;

    public EmailVerificationRequestedOutboxProcessor(
        IOutboxProcessingRepository outboxRepository,
        IEmailSender emailSender,
        IOptions<EmailDeliveryOptions> options,
        ILogger<EmailVerificationRequestedOutboxProcessor> logger)
    {
        _outboxRepository = outboxRepository;
        _emailSender = emailSender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var messages = await _outboxRepository.ClaimAvailableAsync(
            EventType,
            _options.BatchSize,
            _options.MaxRetries,
            _options.ProcessingTimeoutSeconds,
            cancellationToken);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, cancellationToken);
        }

        return messages.Count;
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<EmailVerificationRequestedPayload>(
                message.Payload,
                JsonOptions);

            if (payload is null || string.IsNullOrWhiteSpace(payload.EmailAddress) || string.IsNullOrWhiteSpace(payload.Token))
            {
                throw new InvalidOperationException("Email verification outbox payload is invalid.");
            }

            await _emailSender.SendAsync(CreateEmailMessage(payload), cancellationToken);
            var published = await _outboxRepository.MarkPublishedAsync(
                message.OutboxId,
                message.ProcessingLeaseExpiresAt,
                cancellationToken);
            if (!published)
            {
                _logger.LogInformation(
                    "Email verification outbox message {OutboxId} was not marked published because its processing lease changed.",
                    message.OutboxId);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Failed to process email verification outbox message {OutboxId}.",
                message.OutboxId);

            var failed = await _outboxRepository.MarkFailedAsync(
                message.OutboxId,
                message.ProcessingLeaseExpiresAt,
                DateTime.UtcNow.AddSeconds(_options.RetryDelaySeconds),
                cancellationToken);
            if (!failed)
            {
                _logger.LogInformation(
                    "Email verification outbox message {OutboxId} was not marked failed because its processing lease changed.",
                    message.OutboxId);
            }
        }
    }

    private EmailMessage CreateEmailMessage(EmailVerificationRequestedPayload payload)
    {
        var verificationUrl = _options.VerificationUrlTemplate.Replace(
            "{token}",
            Uri.EscapeDataString(payload.Token),
            StringComparison.Ordinal);

        var body =
            $"""
            Hello,

            Verify your email address using this link:
            {verificationUrl}

            This verification link expires at {payload.ExpiresAt:O}.
            """;

        return new EmailMessage(
            payload.EmailAddress,
            "Verify your email address",
            body);
    }
}
