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

public class RefreshTokenUseCase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISecureTokenService _secureTokenService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IUserAccountRepository userAccountRepository,
        ICompanyRepository companyRepository,
        IRoleRepository roleRepository,
        IAuditLogRepository auditLogRepository,
        ISecureTokenService secureTokenService,
        IAccessTokenService accessTokenService,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userAccountRepository = userAccountRepository;
        _companyRepository = companyRepository;
        _roleRepository = roleRepository;
        _auditLogRepository = auditLogRepository;
        _secureTokenService = secureTokenService;
        _accessTokenService = accessTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<RefreshTokenResult> ExecuteAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var rawRefreshToken = InputValidation.RequireToken(command.RefreshToken, "refreshToken");
        var nowUtc = _clock.UtcNow;
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(_secureTokenService.HashToken(rawRefreshToken), cancellationToken)
            ?? throw new UnauthorizedException("invalid_refresh_token", "refresh token is invalid");

        if (refreshToken.RevokedAt.HasValue || refreshToken.ExpiresAt <= nowUtc)
        {
            throw new UnauthorizedException("invalid_refresh_token", "refresh token is invalid");
        }

        var user = await _userAccountRepository.GetByIdAsync(refreshToken.UserID, cancellationToken)
            ?? throw new UnauthorizedException("invalid_refresh_token", "refresh token is invalid");

        if (!user.IsActive || user.IsDeleted)
        {
            throw new UnauthorizedException("account_inactive", "user account is not active");
        }

        var company = user.CompanyID.HasValue
            ? await _companyRepository.GetByIdAsync(user.CompanyID.Value, cancellationToken)
                ?? throw new UnauthorizedException("invalid_company_context", "company context is invalid")
            : null;
        var roles = await _roleRepository.GetByUserIdAsync(user.UserID, cancellationToken);
        var accessToken = _accessTokenService.Create(user.UserUUID, company?.CompanyUUID, roles.Select(role => role.Name).ToArray());
        var nextRawRefreshToken = _secureTokenService.GenerateToken();
        var nextRefreshTokenHash = _secureTokenService.HashToken(nextRawRefreshToken);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            await _refreshTokenRepository.RevokeAsync(refreshToken.RefreshTokenID, nextRefreshTokenHash, ct);
            await _refreshTokenRepository.CreateAsync(
                new RefreshToken
                {
                    UserID = user.UserID,
                    TokenHash = nextRefreshTokenHash,
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
                Action = AuditActions.RefreshedToken,
                ActorUserID = user.UserID,
                PayloadJson = JsonSerializer.Serialize(new { company?.CompanyUUID }, SerializerOptions),
                CreatedAt = _clock.UtcNow
            },
            cancellationToken);

        return new RefreshTokenResult(accessToken.Token, nextRawRefreshToken, accessToken.ExpiresInSeconds);
    }
}
