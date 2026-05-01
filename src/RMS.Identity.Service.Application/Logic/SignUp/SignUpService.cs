using System.Text.Json;
using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Contracts.EmailVerification;
using RMS.Identity.Service.Domain.Contracts.Outbox;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.EmailVerification;
using RMS.Identity.Service.Domain.Interfaces.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Outbox;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Domain.Interfaces.SignUp;
using RMS.Identity.Service.Domain.Interfaces.UserAccounts;

namespace RMS.Identity.Service.Application.Logic.SignUp;

public sealed class SignUpService : ISignUpService
{
    private const string Route = "/api/v1/signup";
    private const string Method = "POST";

    private readonly IDatabaseTransactionExecutor _transactionExecutor;
    private readonly IIdempotencyCoordinator _idempotencyCoordinator;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IEmailVerificationRepository _emailVerificationRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITextHasher _textHasher;
    private readonly ILogger<SignUpService> _logger;
    private readonly SignUpValidator _validator = new();

    public SignUpService(
        IDatabaseTransactionExecutor transactionExecutor,
        IIdempotencyCoordinator idempotencyCoordinator,
        IUserAccountRepository userAccountRepository,
        IEmailVerificationRepository emailVerificationRepository,
        IAuditLogRepository auditLogRepository,
        IOutboxRepository outboxRepository,
        IPasswordHasher passwordHasher,
        ITextHasher textHasher,
        ILogger<SignUpService> logger)
    {
        _transactionExecutor = transactionExecutor;
        _idempotencyCoordinator = idempotencyCoordinator;
        _userAccountRepository = userAccountRepository;
        _emailVerificationRepository = emailVerificationRepository;
        _auditLogRepository = auditLogRepository;
        _outboxRepository = outboxRepository;
        _passwordHasher = passwordHasher;
        _textHasher = textHasher;
        _logger = logger;
    }

    public async Task<SignUpUser> ExecuteAsync(SignUpCommand command, CancellationToken cancellationToken)
    {
        _validator.Validate(command);

        var normalizedUsername = EmailAddressValidator.Normalize(command.EmailAddress);
        var displayName = BuildDisplayName(command.FirstName, command.MiddleName, command.LastName);
        var phoneNumber = command.PhoneNumber.Trim();
        var createUserCommand = new CreateUserAccountCommand(
            Guid.NewGuid(),
            normalizedUsername,
            _passwordHasher.Hash(command.Password),
            displayName);

        var verificationToken = Guid.NewGuid().ToString("N");
        var verificationEmailOutboxMessage = new VerificationEmailOutboxMessage(
            createUserCommand.UserUuid,
            normalizedUsername,
            displayName,
            verificationToken);
        var createEmailVerificationCommand = new CreateEmailVerificationCommand(
            0,
            _textHasher.Hash(verificationToken),
            DateTime.UtcNow.AddDays(1));
        var idempotencyRequest = CreateIdempotencyRequest(
            command.IdempotencyKey,
            normalizedUsername,
            command.FirstName,
            command.MiddleName,
            command.LastName,
            phoneNumber);

        var executionResult = await _transactionExecutor.ExecuteAsync(
            async (transaction, ct) =>
            {
                var reservation = await _idempotencyCoordinator.ReserveAsync<SignUpUser>(transaction, idempotencyRequest, ct);
                if (reservation.StoredResponse is not null)
                {
                    return new SignUpExecutionResult(reservation.StoredResponse, false);
                }

                if (await _userAccountRepository.ExistsByUsernameAsync(transaction, normalizedUsername, ct))
                {
                    throw UserAlreadyExists();
                }

                var userId = await _userAccountRepository.CreateAsync(transaction, createUserCommand, ct);
                await _emailVerificationRepository.CreateAsync(
                    transaction,
                    createEmailVerificationCommand with { UserId = userId },
                    ct);

                var createdUser = ToSignUpUser(await _userAccountRepository.GetByIdAsync(transaction, userId, ct));
                await _auditLogRepository.InsertSignUpCreatedAsync(transaction, createdUser, ct);

                await _idempotencyCoordinator.StoreResponseAsync(
                    transaction,
                    idempotencyRequest.Key,
                    201,
                    createdUser,
                    ct);

                return new SignUpExecutionResult(createdUser, true);
            },
            cancellationToken);

        if (executionResult.ShouldEnqueueVerificationEmail)
        {
            await TryEnqueueVerificationEmailAsync(verificationEmailOutboxMessage, executionResult.User, cancellationToken);
        }

        return executionResult.User;
    }

    private IdempotencyRequest CreateIdempotencyRequest(
        Guid idempotencyKey,
        string normalizedUsername,
        string firstName,
        string? middleName,
        string lastName,
        string phoneNumber)
    {
        var requestHash = _textHasher.Hash(JsonSerializer.Serialize(new
        {
            emailAddress = normalizedUsername,
            firstName = firstName.Trim(),
            middleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(),
            lastName = lastName.Trim(),
            phoneNumber
        }));

        return new IdempotencyRequest(idempotencyKey.ToString(), Method, Route, requestHash);
    }

    private static string BuildDisplayName(string firstName, string? middleName, string lastName) =>
        string.Join(
            ' ',
            new[] { firstName, middleName, lastName }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));

    private async Task TryEnqueueVerificationEmailAsync(
        VerificationEmailOutboxMessage message,
        SignUpUser createdUser,
        CancellationToken cancellationToken)
    {
        try
        {
            await _outboxRepository.EnqueueAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Signup committed but failed to enqueue verification email for user {UserUuid}.",
                createdUser.UserUuid);
        }
    }

    private static Exception UserAlreadyExists() =>
        new ServiceException(409, "USER_EXISTS", "Email address already exists.");

    private static SignUpUser ToSignUpUser(UserAccount account) =>
        new(
            account.UserUuid,
            account.Username,
            account.DisplayName,
            "pending",
            account.CreatedAt);

    private sealed record SignUpExecutionResult(SignUpUser User, bool ShouldEnqueueVerificationEmail);
}
