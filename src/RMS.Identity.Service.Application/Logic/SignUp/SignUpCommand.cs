using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Idempotency;
using RMS.Identity.Service.Application.Shared.Validation;
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

namespace RMS.Identity.Service.Application.Logic.SignUp;

public sealed class SignUpCommand : ISignUpCommand
{
    private const string Route = "/api/v1/signup";
    private const string Method = "POST";

    private readonly IDatabaseTransactionExecutor _transactionExecutor;
    private readonly IIdempotencyCoordinator _idempotencyCoordinator;
    private readonly IIdempotencyRequestFactory _idempotencyRequestFactory;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SignUpValidator _validator = new();

    public SignUpCommand(
        IDatabaseTransactionExecutor transactionExecutor,
        IIdempotencyCoordinator idempotencyCoordinator,
        IIdempotencyRequestFactory idempotencyRequestFactory,
        IUserAccountRepository userAccountRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher)
    {
        _transactionExecutor = transactionExecutor;
        _idempotencyCoordinator = idempotencyCoordinator;
        _idempotencyRequestFactory = idempotencyRequestFactory;
        _userAccountRepository = userAccountRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<SignUpCommandResponse> ExecuteAsync(SignUpCommandRequest request, CancellationToken cancellationToken)
    {
        _validator.Validate(request);

        var normalizedUsername = EmailAddressValidator.Normalize(request.EmailAddress);
        var displayName = BuildDisplayName(request.FirstName, request.MiddleName, request.LastName);
        var phoneNumber = request.PhoneNumber.Trim();
        var createUserCommand = new CreateUserAccountCommand(
            Guid.NewGuid(),
            normalizedUsername,
            _passwordHasher.Hash(request.Password),
            displayName);

        var idempotencyRequest = _idempotencyRequestFactory.Create(
            request.IdempotencyKey,
            Method,
            Route,
            request);

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
