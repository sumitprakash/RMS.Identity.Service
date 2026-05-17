using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Application.Commands.SignUp;

public sealed class SignUpCommandHandler : ICommandHandler<SignUpCommandRequest, SignUpCommandResponse>
{
    private readonly IUserAccountReadRepository _userAccountReadRepository;
    private readonly IUserAccountWriteRepository _userAccountWriteRepository;
    private readonly IAuditLogWriteRepository _auditLogWriteRepository;
    private readonly IPasswordHasher _passwordHasher;

    public SignUpCommandHandler(
        IUserAccountReadRepository userAccountReadRepository,
        IUserAccountWriteRepository userAccountWriteRepository,
        IAuditLogWriteRepository auditLogWriteRepository,
        IPasswordHasher passwordHasher)
    {
        _userAccountReadRepository = userAccountReadRepository;
        _userAccountWriteRepository = userAccountWriteRepository;
        _auditLogWriteRepository = auditLogWriteRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<SignUpCommandResponse> HandleAsync(SignUpCommandRequest command, CancellationToken cancellationToken)
    {
        var normalizedUsername = EmailAddressValidator.Normalize(command.EmailAddress);
        var displayName = BuildDisplayName(command.FirstName, command.MiddleName, command.LastName);

        if (await _userAccountReadRepository.ExistsByUsernameAsync(normalizedUsername, cancellationToken))
        {
            throw UserAlreadyExists();
        }

        var createUserCommand = new CreateUserAccountCommand(
            Guid.NewGuid(),
            normalizedUsername,
            _passwordHasher.Hash(command.Password),
            displayName);
        var userId = await _userAccountWriteRepository.CreateAsync(createUserCommand, cancellationToken);
        var account = await _userAccountReadRepository.GetByIdAsync(userId, cancellationToken);
        await _auditLogWriteRepository.InsertSignUpCreatedAsync(account, cancellationToken);

        return new SignUpCommandResponse(
            account.UserUuid,
            account.Username,
            "pending",
            account.CreatedAt
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
}
