using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.Idempotency;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Domain.Interfaces.SignUp;
using RMS.Identity.Service.Domain.Interfaces.UserAccounts;
using System.Text.Json;

namespace RMS.Identity.Service.Application.Logic.SignUp;

public sealed class SignUpCommand : ISignUpCommand
{
    private const string Route = "/api/v1/signup";
    private const string Method = "POST";

    private readonly IDatabaseTransactionExecutor _transactionExecutor;
    private readonly IIdempotencyCoordinator _idempotencyCoordinator;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITextHasher _textHasher;
    private readonly SignUpValidator _validator = new();

    public SignUpCommand(
        IDatabaseTransactionExecutor transactionExecutor,
        IIdempotencyCoordinator idempotencyCoordinator,
        IUserAccountRepository userAccountRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher,
        ITextHasher textHasher)
    {
        _transactionExecutor = transactionExecutor;
        _idempotencyCoordinator = idempotencyCoordinator;
        _userAccountRepository = userAccountRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
        _textHasher = textHasher;
    }

    public async Task<SignUpCommandResponse> ExecuteAsync(SignUpCommandRequest command, CancellationToken cancellationToken)
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
                    return new SignUpExecutionResult(reservation.StoredResponse);
                }

                if (await _userAccountRepository.ExistsByUsernameAsync(transaction, normalizedUsername, ct))
                {
                    throw UserAlreadyExists();
                }

                var userId = await _userAccountRepository.CreateAsync(transaction, createUserCommand, ct);
                var createdUser = ToSignUpUser(await _userAccountRepository.GetByIdAsync(transaction, userId, ct));
                await _auditLogRepository.InsertSignUpCreatedAsync(transaction, createdUser, ct);

                await _idempotencyCoordinator.StoreResponseAsync(
                    transaction,
                    idempotencyRequest.Key,
                    201,
                    createdUser,
                    ct);

                return new SignUpExecutionResult(createdUser);
            },
            cancellationToken);

        return new SignUpCommandResponse(
            executionResult.User.UserUuid,
            executionResult.User.Username,
            executionResult.User.Status,
            executionResult.User.CreatedAt
        );
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

    private static Exception UserAlreadyExists() =>
        new ServiceException(409, "USER_EXISTS", "Email address already exists.");

    private static SignUpUser ToSignUpUser(UserAccount account) =>
        new(
            account.UserUuid,
            account.Username,
            account.DisplayName,
            "pending",
            account.CreatedAt);

    private sealed record SignUpExecutionResult(SignUpUser User);
}
