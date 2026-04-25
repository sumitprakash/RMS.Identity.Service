using System.Text.Json;
using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Interfaces.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Domain.Interfaces.SignUp;

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
    private readonly IVerificationEmailOutboxRepository _verificationEmailOutboxRepository;
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
        IVerificationEmailOutboxRepository verificationEmailOutboxRepository,
        IPasswordHasher passwordHasher,
        ITextHasher textHasher,
        ILogger<SignUpService> logger)
    {
        _transactionExecutor = transactionExecutor;
        _idempotencyCoordinator = idempotencyCoordinator;
        _userAccountRepository = userAccountRepository;
        _emailVerificationRepository = emailVerificationRepository;
        _auditLogRepository = auditLogRepository;
        _verificationEmailOutboxRepository = verificationEmailOutboxRepository;
        _passwordHasher = passwordHasher;
        _textHasher = textHasher;
        _logger = logger;
    }

    public async Task<SignUpUser> ExecuteAsync(SignUpCommand command, CancellationToken cancellationToken)
    {
        _validator.Validate(command);

        var normalizedUsername = EmailAddressValidator.Normalize(command.Username);
        var displayName = string.IsNullOrWhiteSpace(command.DisplayName) ? null : command.DisplayName.Trim();
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
        var idempotencyRequest = CreateIdempotencyRequest(command.IdempotencyKey, normalizedUsername, displayName, command.Phone);

        var executionResult = await _transactionExecutor.ExecuteAsync(
            async (transaction, ct) =>
            {
                if (idempotencyRequest is not null)
                {
                    var reservation = await _idempotencyCoordinator.ReserveAsync<SignUpUser>(transaction, idempotencyRequest, ct);
                    if (reservation.StoredResponse is not null)
                    {
                        return new SignUpExecutionResult(reservation.StoredResponse, false);
                    }
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

                var createdUser = await _userAccountRepository.GetSignUpUserAsync(transaction, userId, ct);
                await _auditLogRepository.InsertSignUpCreatedAsync(transaction, createdUser, ct);

                if (idempotencyRequest is not null)
                {
                    await _idempotencyCoordinator.StoreResponseAsync(
                        transaction,
                        idempotencyRequest.Key,
                        201,
                        createdUser,
                        ct);
                }

                return new SignUpExecutionResult(createdUser, true);
            },
            cancellationToken);

        if (executionResult.ShouldEnqueueVerificationEmail)
        {
            await TryEnqueueVerificationEmailAsync(verificationEmailOutboxMessage, executionResult.User, cancellationToken);
        }

        return executionResult.User;
    }

    private IdempotencyRequest? CreateIdempotencyRequest(
        string? idempotencyKey,
        string normalizedUsername,
        string? displayName,
        string? phone)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var requestHash = _textHasher.Hash(JsonSerializer.Serialize(new
        {
            username = normalizedUsername,
            displayName,
            phone
        }));

        return new IdempotencyRequest(idempotencyKey, Method, Route, requestHash);
    }

    private async Task TryEnqueueVerificationEmailAsync(
        VerificationEmailOutboxMessage message,
        SignUpUser createdUser,
        CancellationToken cancellationToken)
    {
        try
        {
            await _verificationEmailOutboxRepository.EnqueueAsync(message, cancellationToken);
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
        new ServiceException(409, "USER_EXISTS", "Username already exists.");

    private sealed record SignUpExecutionResult(SignUpUser User, bool ShouldEnqueueVerificationEmail);
}
