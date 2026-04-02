using System.Text.Json;
using RMS.Identity.Service.Application.Identity.Internal;
using RMS.Identity.Service.Application.Identity.Requests;
using RMS.Identity.Service.Domain.Constants;
using RMS.Identity.Service.Domain.Entities;
using RMS.Identity.Service.Domain.Exceptions;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Domain.Interfaces.System;

namespace RMS.Identity.Service.Application.Identity;

public class VerifyEmailUseCase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IEmailVerificationRepository _emailVerificationRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISecureTokenService _secureTokenService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyEmailUseCase(
        IEmailVerificationRepository emailVerificationRepository,
        IUserAccountRepository userAccountRepository,
        IAuditLogRepository auditLogRepository,
        ISecureTokenService secureTokenService,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _emailVerificationRepository = emailVerificationRepository;
        _userAccountRepository = userAccountRepository;
        _auditLogRepository = auditLogRepository;
        _secureTokenService = secureTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(VerifyEmailCommand command, CancellationToken cancellationToken = default)
    {
        var token = InputValidation.RequireToken(command.Token, "token");
        var tokenHash = _secureTokenService.HashToken(token);
        EmailVerification verification = null!;
        UserAccount user = null!;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            verification = await _emailVerificationRepository.GetActiveByTokenHashAsync(
                tokenHash,
                VerificationPurposes.EmailVerification,
                ct) ?? throw new ValidationException("invalid_token", "token is invalid or expired");

            if (verification.ExpiresAt <= _clock.UtcNow)
            {
                throw new ValidationException("invalid_token", "token is invalid or expired");
            }

            user = await _userAccountRepository.GetByIdAsync(verification.UserID, ct)
                ?? throw new NotFoundException("user_not_found", "user not found");

            await _emailVerificationRepository.MarkConsumedAsync(verification.EmailVerificationID, ct);
            await _userAccountRepository.MarkEmailVerifiedAsync(user.UserID, ct);
        }, cancellationToken);

        await _auditLogRepository.CreateAsync(
            new AuditLog
            {
                TableName = nameof(UserAccount),
                RecordId = user.UserUUID.ToString(),
                Action = AuditActions.EmailVerified,
                ActorUserID = user.UserID,
                PayloadJson = JsonSerializer.Serialize(new { user.Username }, SerializerOptions),
                CreatedAt = _clock.UtcNow
            },
            cancellationToken);
    }
}
