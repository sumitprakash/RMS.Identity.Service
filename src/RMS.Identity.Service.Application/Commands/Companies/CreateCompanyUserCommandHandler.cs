using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Contracts.UserAccounts;
using RMS.Identity.Service.Domain.Contracts.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class CreateCompanyUserCommandHandler : ICommandHandler<CreateCompanyUserCommandRequest, CreateCompanyUserCommandResponse>
{
    private const string EmailVerificationPurpose = "email_verification";
    private static readonly TimeSpan EmailVerificationTokenLifetime = TimeSpan.FromHours(24);

    private readonly ICompanyReadRepository _companyReadRepository;
    private readonly IUserAccountReadRepository _userAccountReadRepository;
    private readonly IUserAccountWriteRepository _userAccountWriteRepository;
    private readonly ICompanyUserWriteRepository _companyUserWriteRepository;
    private readonly IEmailVerificationWriteRepository _emailVerificationWriteRepository;
    private readonly IOutboxWriteRepository _outboxWriteRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITextHasher _textHasher;
    private readonly IAuditLogWriteRepository _auditLogWriteRepository;
    private readonly ILogger<CreateCompanyUserCommandHandler> _logger;

    public CreateCompanyUserCommandHandler(
        ICompanyReadRepository companyReadRepository,
        IUserAccountReadRepository userAccountReadRepository,
        IUserAccountWriteRepository userAccountWriteRepository,
        ICompanyUserWriteRepository companyUserWriteRepository,
        IEmailVerificationWriteRepository emailVerificationWriteRepository,
        IOutboxWriteRepository outboxWriteRepository,
        IPasswordHasher passwordHasher,
        ITextHasher textHasher,
        IAuditLogWriteRepository auditLogWriteRepository,
        ILogger<CreateCompanyUserCommandHandler> logger)
    {
        _companyReadRepository = companyReadRepository;
        _userAccountReadRepository = userAccountReadRepository;
        _userAccountWriteRepository = userAccountWriteRepository;
        _companyUserWriteRepository = companyUserWriteRepository;
        _emailVerificationWriteRepository = emailVerificationWriteRepository;
        _outboxWriteRepository = outboxWriteRepository;
        _passwordHasher = passwordHasher;
        _textHasher = textHasher;
        _auditLogWriteRepository = auditLogWriteRepository;
        _logger = logger;
    }

    public async Task<CreateCompanyUserCommandResponse> HandleAsync(
        CreateCompanyUserCommandRequest command,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = EmailAddressValidator.Normalize(command.Username);
        if (normalizedUsername.Length > 150 || (command.DisplayName?.Trim().Length ?? 0) > 255)
        {
            throw new ApplicationServiceException(
                ServiceStatusErrorCodes.BadRequest,
                "Username or display name exceeds the supported length.");
        }

        if (await _userAccountReadRepository.ExistsByUsernameAsync(normalizedUsername, cancellationToken))
        {
            _logger.LogWarning("Company user creation rejected because username already exists for company {CompanyUuid}.", command.CompanyUuid);
            throw new ApplicationServiceException(ServiceErrorDefinitions.Users.UserExists);
        }

        var companyRole = command.CompanyRole.ToStorageValue();
        var company = await _companyReadRepository.GetByUuidAsync(command.CompanyUuid, cancellationToken);
        if (company.IsDeleted)
        {
            _logger.LogWarning("Company user creation rejected because company {CompanyUuid} is deleted.", command.CompanyUuid);
            throw new ApplicationServiceException(ServiceErrorDefinitions.Companies.CompanyNotFound);
        }

        var userId = await _userAccountWriteRepository.CreateAsync(
            new CreateUserAccountCommand(
                Guid.NewGuid(),
                normalizedUsername,
                _passwordHasher.Hash(CreateTemporaryPassword()),
                TrimToNull(command.DisplayName),
                PhoneNumber: null,
                PasswordSetupRequired: true),
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

        if (command.ActorUserUuid != Guid.Empty)
        {
            await _auditLogWriteRepository.InsertCompanyUserChangedAsync(
                "company_user_created",
                command.ActorUserUuid,
                command.CompanyUuid,
                user.UserUuid,
                previousCompanyRole: null,
                previousMembershipStatus: null,
                companyRole,
                "active",
                cancellationToken);
        }
        _logger.LogInformation(
            "Created company user {UserUuid} in company {CompanyUuid} with role {CompanyRole}.",
            user.UserUuid,
            command.CompanyUuid,
            companyRole);

        return new CreateCompanyUserCommandResponse(
            user.UserUuid,
            user.Username,
            user.DisplayName,
            Array.Empty<string>(),
            companyRole,
            "pending",
            user.CreatedAt);
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
