using System.Security.Cryptography;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Contracts.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class CreateCompanyUserCommandHandler : ICommandHandler<CreateCompanyUserCommandRequest, CreateCompanyUserCommandResponse>
{
    private const string EmailVerificationPurpose = "email_verification";
    private static readonly TimeSpan EmailVerificationTokenLifetime = TimeSpan.FromHours(24);
    private static readonly string[] AllowedCompanyRoles = ["OWNER", "ADMIN", "MEMBER"];

    private readonly ICompanyReadRepository _companyReadRepository;
    private readonly IUserAccountReadRepository _userAccountReadRepository;
    private readonly IUserAccountWriteRepository _userAccountWriteRepository;
    private readonly ICompanyUserWriteRepository _companyUserWriteRepository;
    private readonly IEmailVerificationWriteRepository _emailVerificationWriteRepository;
    private readonly IOutboxWriteRepository _outboxWriteRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITextHasher _textHasher;

    public CreateCompanyUserCommandHandler(
        ICompanyReadRepository companyReadRepository,
        IUserAccountReadRepository userAccountReadRepository,
        IUserAccountWriteRepository userAccountWriteRepository,
        ICompanyUserWriteRepository companyUserWriteRepository,
        IEmailVerificationWriteRepository emailVerificationWriteRepository,
        IOutboxWriteRepository outboxWriteRepository,
        IPasswordHasher passwordHasher,
        ITextHasher textHasher)
    {
        _companyReadRepository = companyReadRepository;
        _userAccountReadRepository = userAccountReadRepository;
        _userAccountWriteRepository = userAccountWriteRepository;
        _companyUserWriteRepository = companyUserWriteRepository;
        _emailVerificationWriteRepository = emailVerificationWriteRepository;
        _outboxWriteRepository = outboxWriteRepository;
        _passwordHasher = passwordHasher;
        _textHasher = textHasher;
    }

    public async Task<CreateCompanyUserCommandResponse> HandleAsync(
        CreateCompanyUserCommandRequest command,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = EmailAddressValidator.Normalize(command.Username);
        if (await _userAccountReadRepository.ExistsByUsernameAsync(normalizedUsername, cancellationToken))
        {
            throw new ServiceException(409, "USER_EXISTS", "Email address already exists.");
        }

        var companyRole = NormalizeCompanyRole(command.CompanyRole);
        var company = await _companyReadRepository.GetByUuidAsync(command.CompanyUuid, cancellationToken);
        if (company.IsDeleted)
        {
            throw new ServiceException(404, "COMPANY_NOT_FOUND", "Company could not be found.");
        }

        var userId = await _userAccountWriteRepository.CreateAsync(
            new CreateUserAccountCommand(
                Guid.NewGuid(),
                normalizedUsername,
                _passwordHasher.Hash(CreateTemporaryPassword()),
                TrimToNull(command.DisplayName)),
            cancellationToken);
        var user = await _userAccountReadRepository.GetByIdAsync(userId, cancellationToken);
        var verificationToken = CreateVerificationToken();
        var verificationExpiresAt = DateTime.UtcNow.Add(EmailVerificationTokenLifetime);

        await _emailVerificationWriteRepository.CreateAsync(
            new CreateEmailVerificationCommand(
                user.UserId,
                _textHasher.Hash(verificationToken),
                EmailVerificationPurpose,
                verificationExpiresAt),
            cancellationToken);
        await _outboxWriteRepository.InsertEmailVerificationRequestedAsync(
            user,
            verificationToken,
            verificationExpiresAt,
            cancellationToken);

        await _companyUserWriteRepository.CreateAsync(
            new CreateCompanyUserCommand(company.CompanyId, user.UserId, companyRole, "active"),
            cancellationToken);

        return new CreateCompanyUserCommandResponse(
            user.UserUuid,
            user.Username,
            user.DisplayName,
            Array.Empty<string>(),
            companyRole,
            "pending",
            user.CreatedAt);
    }

    private static string NormalizeCompanyRole(string companyRole)
    {
        var normalized = companyRole.Trim().ToUpperInvariant();
        if (!AllowedCompanyRoles.Contains(normalized, StringComparer.Ordinal))
        {
            throw new ServiceException(400, "VALIDATION_ERROR", "Company role must be OWNER, ADMIN, or MEMBER.");
        }

        return normalized;
    }

    private static string CreateTemporaryPassword() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string CreateVerificationToken() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
