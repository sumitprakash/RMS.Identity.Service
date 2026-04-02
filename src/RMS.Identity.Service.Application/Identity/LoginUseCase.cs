using System.Text.Json;
using RMS.Identity.Service.Application.Identity.Internal;
using RMS.Identity.Service.Application.Identity.Requests;
using RMS.Identity.Service.Application.Identity.Results;
using RMS.Identity.Service.Domain.Constants;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Domain.Exceptions;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Domain.Interfaces.System;

namespace RMS.Identity.Service.Application.Identity;

public class LoginUseCase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecureTokenService _secureTokenService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public LoginUseCase(
        IUserAccountRepository userAccountRepository,
        ICompanyRepository companyRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher,
        ISecureTokenService secureTokenService,
        IAccessTokenService accessTokenService,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _userAccountRepository = userAccountRepository;
        _companyRepository = companyRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
        _secureTokenService = secureTokenService;
        _accessTokenService = accessTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResult> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var username = InputValidation.RequireEmail(command.Username, "username");
        InputValidation.RequireToken(command.Password, "password");
        var user = await _userAccountRepository.GetByUsernameAsync(username, cancellationToken)
            ?? throw new UnauthorizedException("invalid_credentials", "username or password is invalid");

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            await _userAccountRepository.RecordFailedLoginAsync(user.UserID, cancellationToken);
            throw new UnauthorizedException("invalid_credentials", "username or password is invalid");
        }

        if (!user.IsActive || user.IsDeleted || (user.LockedUntil.HasValue && user.LockedUntil.Value > _clock.UtcNow))
        {
            throw new UnauthorizedException("account_inactive", "user account is not active");
        }

        Company? company = null;
        if (command.CompanyUuid.HasValue)
        {
            company = await _companyRepository.GetByUuidAsync(command.CompanyUuid.Value, cancellationToken)
                ?? throw new UnauthorizedException("invalid_company_context", "company context is invalid");

            if (user.CompanyID != company.CompanyID)
            {
                throw new UnauthorizedException("invalid_company_context", "company context is invalid");
            }
        }
        else if (user.CompanyID.HasValue)
        {
            company = await _companyRepository.GetByIdAsync(user.CompanyID.Value, cancellationToken)
                ?? throw new UnauthorizedException("invalid_company_context", "company context is invalid");
        }

        var roles = await _roleRepository.GetByUserIdAsync(user.UserID, cancellationToken);
        var accessToken = _accessTokenService.Create(user.UserUUID, company?.CompanyUUID, roles.Select(role => role.Name).ToArray());
        var rawRefreshToken = _secureTokenService.GenerateToken();
        var refreshTokenHash = _secureTokenService.HashToken(rawRefreshToken);
        var nowUtc = _clock.UtcNow;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            await _userAccountRepository.RecordSuccessfulLoginAsync(user.UserID, nowUtc, ct);
            await _refreshTokenRepository.CreateAsync(
                new RefreshToken
                {
                    UserID = user.UserID,
                    TokenHash = refreshTokenHash,
                    ExpiresAt = nowUtc.AddDays(30),
                    CreatedAt = nowUtc
                },
                ct);
        }, cancellationToken);

        await _auditLogRepository.CreateAsync(
            new AuditLog
            {
                TableName = nameof(UserAccount),
                RecordId = user.UserUUID.ToString(),
                Action = AuditActions.LoggedIn,
                ActorUserID = user.UserID,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    CompanyUuid = company?.CompanyUUID,
                    RoleCount = roles.Count
                }, SerializerOptions),
                CreatedAt = _clock.UtcNow
            },
            cancellationToken);

        return new LoginResult(
            accessToken.Token,
            rawRefreshToken,
            accessToken.ExpiresInSeconds,
            "Bearer",
            UserMappings.ToResult(user, roles, nowUtc));
    }
}
