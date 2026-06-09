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
        var endpointClient = new FakeEmailVerificationEndpointClient();
        var processor = CreateProcessor(outboxRepository, emailSender, endpointClient);

        var processed = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Single(emailSender.SentMessages);
        Assert.Empty(endpointClient.VerifiedTokens);
        Assert.Equal("alice@example.com", emailSender.SentMessages[0].To);
        Assert.Contains("https://app.example.com/verify-email?token=token%2Bvalue", emailSender.SentMessages[0].Body);
        Assert.Equal(100, outboxRepository.PublishedOutboxId);
        Assert.Equal(outboxRepository.Messages.Single().ProcessingLeaseExpiresAt, outboxRepository.PublishedProcessingLeaseExpiresAt);
        Assert.Null(outboxRepository.FailedOutboxId);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenEndpointAutoVerifyEnabled_CallsVerifyEndpointAndMarksPublished()
    {
        var outboxRepository = new FakeOutboxProcessingRepository(CreateMessage());
        var emailSender = new FakeEmailSender();
        var endpointClient = new FakeEmailVerificationEndpointClient();
        var processor = CreateProcessor(
            outboxRepository,
            emailSender,
            endpointClient,
            autoVerifyByEndpoint: true);

        var processed = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Empty(emailSender.SentMessages);
        Assert.Single(endpointClient.VerifiedTokens);
        Assert.Equal("token+value", endpointClient.VerifiedTokens[0]);
        Assert.Equal(100, outboxRepository.PublishedOutboxId);
        Assert.Null(outboxRepository.FailedOutboxId);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenEmailSendFails_MarksMessageFailedForRetry()
    {
        var outboxRepository = new FakeOutboxProcessingRepository(CreateMessage());
        var emailSender = new FakeEmailSender(sendFails: true);
        var endpointClient = new FakeEmailVerificationEndpointClient();
        var processor = CreateProcessor(outboxRepository, emailSender, endpointClient);

        var processed = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Empty(emailSender.SentMessages);
        Assert.Empty(endpointClient.VerifiedTokens);
        Assert.Null(outboxRepository.PublishedOutboxId);
        Assert.Equal(100, outboxRepository.FailedOutboxId);
        Assert.Equal(outboxRepository.Messages.Single().ProcessingLeaseExpiresAt, outboxRepository.FailedProcessingLeaseExpiresAt);
        Assert.True(outboxRepository.FailedAvailableAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithInvalidPayload_MarksMessageFailed()
    {
        var outboxRepository = new FakeOutboxProcessingRepository(new OutboxMessage(
            100,
            EmailVerificationRequestedOutboxProcessor.EventType,
            "{}",
            0,
            DateTime.UtcNow.AddMinutes(5)));
        var emailSender = new FakeEmailSender();
        var endpointClient = new FakeEmailVerificationEndpointClient();
        var processor = CreateProcessor(outboxRepository, emailSender, endpointClient);

        var processed = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Empty(emailSender.SentMessages);
        Assert.Empty(endpointClient.VerifiedTokens);
        Assert.Null(outboxRepository.PublishedOutboxId);
        Assert.Equal(100, outboxRepository.FailedOutboxId);
    }

    private static EmailVerificationRequestedOutboxProcessor CreateProcessor(
        FakeOutboxProcessingRepository outboxRepository,
        FakeEmailSender emailSender,
        FakeEmailVerificationEndpointClient endpointClient,
        bool autoVerifyByEndpoint = false) =>
        new(
            outboxRepository,
            emailSender,
            endpointClient,
            Options.Create(new EmailDeliveryOptions
            {
                Enabled = true,
                AutoVerifyByEndpoint = autoVerifyByEndpoint,
                VerifyEmailEndpointUrl = "http://localhost:5000/api/v1/users/verify-email",
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
            0,
            DateTime.UtcNow.AddMinutes(5));

    private sealed class FakeOutboxProcessingRepository : IOutboxProcessingRepository
    {
        public FakeOutboxProcessingRepository(params OutboxMessage[] messages)
        {
            Messages = messages;
        }

        public IReadOnlyCollection<OutboxMessage> Messages { get; }

        public long? PublishedOutboxId { get; private set; }

        public DateTime? PublishedProcessingLeaseExpiresAt { get; private set; }

        public long? FailedOutboxId { get; private set; }

        public DateTime? FailedProcessingLeaseExpiresAt { get; private set; }

        public DateTime? FailedAvailableAt { get; private set; }

        public Task<IReadOnlyCollection<OutboxMessage>> ClaimAvailableAsync(
            string eventType,
            int batchSize,
            int maxRetries,
            int processingTimeoutSeconds,
            CancellationToken cancellationToken) =>
            Task.FromResult(Messages);

        public Task<bool> MarkPublishedAsync(
            long outboxId,
            DateTime processingLeaseExpiresAt,
            CancellationToken cancellationToken)
        {
            PublishedOutboxId = outboxId;
            PublishedProcessingLeaseExpiresAt = processingLeaseExpiresAt;
            return Task.FromResult(true);
        }

        public Task<bool> MarkFailedAsync(
            long outboxId,
            DateTime processingLeaseExpiresAt,
            DateTime availableAt,
            CancellationToken cancellationToken)
        {
            FailedOutboxId = outboxId;
            FailedProcessingLeaseExpiresAt = processingLeaseExpiresAt;
            FailedAvailableAt = availableAt;
            return Task.FromResult(true);
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

    private sealed class FakeEmailVerificationEndpointClient : IEmailVerificationEndpointClient
    {
        public List<string> VerifiedTokens { get; } = new();

        public Task VerifyAsync(string token, CancellationToken cancellationToken)
        {
            VerifiedTokens.Add(token);
            return Task.CompletedTask;
        }
    }
}
