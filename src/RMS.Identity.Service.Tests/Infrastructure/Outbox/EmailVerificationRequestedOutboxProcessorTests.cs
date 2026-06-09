using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Domain.Entities.Outbox;
using RMS.Identity.Service.Domain.Interfaces.Notifications;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;
using RMS.Identity.Service.Infrastructure.Notifications;
using RMS.Identity.Service.Infrastructure.Outbox;

namespace RMS.Identity.Service.Tests.Infrastructure.Outbox;

public sealed class EmailVerificationRequestedOutboxProcessorTests
{
    [Fact]
    public async Task ProcessBatchAsync_WithEmailVerificationMessage_SendsEmailAndMarksPublished()
    {
        var outboxRepository = new FakeOutboxProcessingRepository(CreateMessage());
        var emailSender = new FakeEmailSender();
        var processor = CreateProcessor(outboxRepository, emailSender);

        var processed = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Single(emailSender.SentMessages);
        Assert.Equal("alice@example.com", emailSender.SentMessages[0].To);
        Assert.Contains("https://app.example.com/verify-email?token=token%2Bvalue", emailSender.SentMessages[0].Body);
        Assert.Equal(100, outboxRepository.PublishedOutboxId);
        Assert.Null(outboxRepository.FailedOutboxId);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenEmailSendFails_MarksMessageFailedForRetry()
    {
        var outboxRepository = new FakeOutboxProcessingRepository(CreateMessage());
        var emailSender = new FakeEmailSender(sendFails: true);
        var processor = CreateProcessor(outboxRepository, emailSender);

        var processed = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Empty(emailSender.SentMessages);
        Assert.Null(outboxRepository.PublishedOutboxId);
        Assert.Equal(100, outboxRepository.FailedOutboxId);
        Assert.True(outboxRepository.FailedAvailableAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithInvalidPayload_MarksMessageFailed()
    {
        var outboxRepository = new FakeOutboxProcessingRepository(new OutboxMessage(
            100,
            EmailVerificationRequestedOutboxProcessor.EventType,
            "{}",
            0));
        var emailSender = new FakeEmailSender();
        var processor = CreateProcessor(outboxRepository, emailSender);

        var processed = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Empty(emailSender.SentMessages);
        Assert.Null(outboxRepository.PublishedOutboxId);
        Assert.Equal(100, outboxRepository.FailedOutboxId);
    }

    private static EmailVerificationRequestedOutboxProcessor CreateProcessor(
        FakeOutboxProcessingRepository outboxRepository,
        FakeEmailSender emailSender) =>
        new(
            outboxRepository,
            emailSender,
            Options.Create(new EmailDeliveryOptions
            {
                Enabled = true,
                BatchSize = 10,
                MaxRetries = 5,
                RetryDelaySeconds = 300,
                ProcessingTimeoutSeconds = 300,
                VerificationUrlTemplate = "https://app.example.com/verify-email?token={token}"
            }),
            NullLogger<EmailVerificationRequestedOutboxProcessor>.Instance);

    private static OutboxMessage CreateMessage() =>
        new(
            100,
            EmailVerificationRequestedOutboxProcessor.EventType,
            JsonSerializer.Serialize(new
            {
                UserUuid = Guid.NewGuid(),
                EmailAddress = "alice@example.com",
                Token = "token+value",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            }),
            0);

    private sealed class FakeOutboxProcessingRepository : IOutboxProcessingRepository
    {
        private readonly IReadOnlyCollection<OutboxMessage> _messages;

        public FakeOutboxProcessingRepository(params OutboxMessage[] messages)
        {
            _messages = messages;
        }

        public long? PublishedOutboxId { get; private set; }

        public long? FailedOutboxId { get; private set; }

        public DateTime? FailedAvailableAt { get; private set; }

        public Task<IReadOnlyCollection<OutboxMessage>> ClaimAvailableAsync(
            string eventType,
            int batchSize,
            int maxRetries,
            int processingTimeoutSeconds,
            CancellationToken cancellationToken) =>
            Task.FromResult(_messages);

        public Task MarkPublishedAsync(long outboxId, CancellationToken cancellationToken)
        {
            PublishedOutboxId = outboxId;
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(
            long outboxId,
            DateTime availableAt,
            CancellationToken cancellationToken)
        {
            FailedOutboxId = outboxId;
            FailedAvailableAt = availableAt;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        private readonly bool _sendFails;

        public FakeEmailSender(bool sendFails = false)
        {
            _sendFails = sendFails;
        }

        public List<EmailMessage> SentMessages { get; } = new();

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (_sendFails)
            {
                throw new InvalidOperationException("SMTP unavailable.");
            }

            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
