using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Entities.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Application.Commands.SignUp;

public sealed class SignUpCommandHandler : ICommandHandler<SignUpCommandRequest, SignUpCommandResponse>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SignUpValidator _validator = new();

    public SignUpCommandHandler(
        IUserAccountRepository userAccountRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher)
    {
        _userAccountRepository = userAccountRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<SignUpCommandResponse> HandleAsync(SignUpCommandRequest command, CancellationToken cancellationToken)
    {
        _validator.Validate(command);

        var normalizedUsername = EmailAddressValidator.Normalize(command.EmailAddress);
        var displayName = BuildDisplayName(command.FirstName, command.MiddleName, command.LastName);

        if (await _userAccountRepository.ExistsByUsernameAsync(normalizedUsername, cancellationToken))
        {
            throw UserAlreadyExists();
        }

        var createUserCommand = new CreateUserAccountCommand(
            Guid.NewGuid(),
            normalizedUsername,
            _passwordHasher.Hash(command.Password),
            displayName);
        var userId = await _userAccountRepository.CreateAsync(createUserCommand, cancellationToken);
        var user = ToSignUpUser(await _userAccountRepository.GetByIdAsync(userId, cancellationToken));
        await _auditLogRepository.InsertSignUpCreatedAsync(user, cancellationToken);

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
