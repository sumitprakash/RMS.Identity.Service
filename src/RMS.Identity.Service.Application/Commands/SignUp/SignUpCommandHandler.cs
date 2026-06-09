using System.Security.Cryptography;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Contracts.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Application.Commands.SignUp;

public sealed class SignUpCommandHandler : ICommandHandler<SignUpCommandRequest, SignUpCommandResponse>
{
    private const string EmailVerificationPurpose = "email_verification";
    private static readonly TimeSpan EmailVerificationTokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserAccountReadRepository _userAccountReadRepository;
    private readonly IUserAccountWriteRepository _userAccountWriteRepository;
    private readonly IAuditLogWriteRepository _auditLogWriteRepository;
    private readonly IEmailVerificationWriteRepository _emailVerificationWriteRepository;
    private readonly IOutboxWriteRepository _outboxWriteRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITextHasher _textHasher;

    public SignUpCommandHandler(
        IUserAccountReadRepository userAccountReadRepository,
        IUserAccountWriteRepository userAccountWriteRepository,
        IAuditLogWriteRepository auditLogWriteRepository,
        IEmailVerificationWriteRepository emailVerificationWriteRepository,
        IOutboxWriteRepository outboxWriteRepository,
        IPasswordHasher passwordHasher,
        ITextHasher textHasher)
    {
        _userAccountReadRepository = userAccountReadRepository;
        _userAccountWriteRepository = userAccountWriteRepository;
        _auditLogWriteRepository = auditLogWriteRepository;
        _emailVerificationWriteRepository = emailVerificationWriteRepository;
        _outboxWriteRepository = outboxWriteRepository;
        _passwordHasher = passwordHasher;
        _textHasher = textHasher;
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
        var verificationToken = CreateVerificationToken();
        var verificationExpiresAt = DateTime.UtcNow.Add(EmailVerificationTokenLifetime);
        await _emailVerificationWriteRepository.CreateAsync(
            new CreateEmailVerificationCommand(
                account.UserId,
                _textHasher.Hash(verificationToken),
                EmailVerificationPurpose,
                verificationExpiresAt),
            cancellationToken);
        await _outboxWriteRepository.InsertEmailVerificationRequestedAsync(
            account,
            verificationToken,
            verificationExpiresAt,
            cancellationToken);
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

    private static string CreateVerificationToken() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
