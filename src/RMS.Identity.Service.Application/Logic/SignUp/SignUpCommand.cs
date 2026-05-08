using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Domain.Interfaces.SignUp;
using RMS.Identity.Service.Domain.Interfaces.UserAccounts;

namespace RMS.Identity.Service.Application.Logic.SignUp;

public sealed class SignUpCommand : ISignUpCommand
{
    private readonly IDatabaseTransactionAccessor _transactionAccessor;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SignUpValidator _validator = new();

    public SignUpCommand(
        IDatabaseTransactionAccessor transactionAccessor,
        IUserAccountRepository userAccountRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher)
    {
        _transactionAccessor = transactionAccessor;
        _userAccountRepository = userAccountRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<SignUpCommandResponse> ExecuteAsync(SignUpCommandRequest request, CancellationToken cancellationToken)
    {
        _validator.Validate(request);

        var normalizedUsername = EmailAddressValidator.Normalize(request.EmailAddress);
        var displayName = BuildDisplayName(request.FirstName, request.MiddleName, request.LastName);
        var transaction = _transactionAccessor.GetCurrent();

        if (await _userAccountRepository.ExistsByUsernameAsync(transaction, normalizedUsername, cancellationToken))
        {
            throw UserAlreadyExists();
        }

        var createUserCommand = new CreateUserAccountCommand(
            Guid.NewGuid(),
            normalizedUsername,
            _passwordHasher.Hash(request.Password),
            displayName);
        var userId = await _userAccountRepository.CreateAsync(transaction, createUserCommand, cancellationToken);
        var user = ToSignUpUser(await _userAccountRepository.GetByIdAsync(transaction, userId, cancellationToken));
        await _auditLogRepository.InsertSignUpCreatedAsync(transaction, user, cancellationToken);

        return new SignUpCommandResponse(
            user.UserUuid,
            user.Username,
            user.Status,
            user.CreatedAt
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
}
