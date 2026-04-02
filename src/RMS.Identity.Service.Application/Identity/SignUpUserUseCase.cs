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

public class SignUpUserUseCase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IEmailVerificationRepository _emailVerificationRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecureTokenService _secureTokenService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public SignUpUserUseCase(
        IUserAccountRepository userAccountRepository,
        IEmailVerificationRepository emailVerificationRepository,
        IIdempotencyRepository idempotencyRepository,
        IOutboxRepository outboxRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher,
        ISecureTokenService secureTokenService,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _userAccountRepository = userAccountRepository;
        _emailVerificationRepository = emailVerificationRepository;
        _idempotencyRepository = idempotencyRepository;
        _outboxRepository = outboxRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
        _secureTokenService = secureTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserResult> ExecuteAsync(SignUpUserCommand command, CancellationToken cancellationToken = default)
    {
        var username = InputValidation.RequireEmail(command.Username, "username");
        InputValidation.RequirePassword(command.Password);
        var idempotencyKey = string.IsNullOrWhiteSpace(command.IdempotencyKey) ? null : command.IdempotencyKey.Trim();
        var requestHash = RequestHashing.Compute(new
        {
            username,
            command.Password,
            command.DisplayName,
            command.Phone
        });

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _idempotencyRepository.GetAsync(idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.Method, "POST", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(existing.Route, "/api/v1/signup", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                {
                    throw new ConflictException("idempotency_conflict", "Idempotency-Key cannot be reused with a different signup request");
                }

                if (existing.ResponseBody is not null)
                {
                    var cached = JsonSerializer.Deserialize<UserResult>(existing.ResponseBody, SerializerOptions);
                    if (cached is not null)
                    {
                        return cached;
                    }
                }
            }
        }

        UserResult result = null!;
        UserAccount createdUser = null!;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var existingUser = await _userAccountRepository.GetByUsernameAsync(username, ct);
            if (existingUser is not null)
            {
                throw new ConflictException("user_exists", "username already exists");
            }

            var nowUtc = _clock.UtcNow;
            createdUser = new UserAccount
            {
                UserUUID = Guid.NewGuid(),
                Username = username,
                PasswordHash = _passwordHasher.Hash(command.Password),
                DisplayName = string.IsNullOrWhiteSpace(command.DisplayName) ? null : command.DisplayName.Trim(),
                CompanyID = null,
                EmailVerified = false,
                IsActive = true,
                CreatedAt = nowUtc
            };

            createdUser.UserID = await _userAccountRepository.CreateAsync(createdUser, ct);

            var rawVerificationToken = _secureTokenService.GenerateToken();
            var verification = new EmailVerification
            {
                UserID = createdUser.UserID,
                TokenHash = _secureTokenService.HashToken(rawVerificationToken),
                Purpose = VerificationPurposes.EmailVerification,
                ExpiresAt = nowUtc.AddHours(24),
                CreatedAt = nowUtc,
                Consumed = false
            };

            await _emailVerificationRepository.CreateAsync(verification, ct);
            await _outboxRepository.CreateAsync(
                new OutboxMessage
                {
                    EventType = OutboxEventTypes.EmailVerificationRequested,
                    AggregateType = nameof(UserAccount),
                    AggregateUUID = createdUser.UserUUID,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        createdUser.UserUUID,
                        createdUser.Username,
                        VerificationPurpose = VerificationPurposes.EmailVerification,
                        VerificationRecordId = verification.EmailVerificationID
                    }, SerializerOptions),
                    Status = "pending",
                    CreatedAt = nowUtc,
                    AvailableAt = nowUtc
                },
                ct);

            result = UserMappings.ToResult(createdUser, Array.Empty<Role>(), nowUtc);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _idempotencyRepository.CreateAsync(
                    new IdempotencyKeyRecord
                    {
                        KeyValue = idempotencyKey,
                        Method = "POST",
                        Route = "/api/v1/signup",
                        RequestHash = requestHash,
                        ResponseCode = 201,
                        ResponseBody = JsonSerializer.Serialize(result, SerializerOptions),
                        CreatedAt = nowUtc
                    },
                    ct);
            }
        }, cancellationToken);

        await _auditLogRepository.CreateAsync(
            new AuditLog
            {
                TableName = nameof(UserAccount),
                RecordId = createdUser.UserUUID.ToString(),
                Action = AuditActions.Created,
                ActorUserID = createdUser.UserID,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    createdUser.Username,
                    createdUser.DisplayName
                }, SerializerOptions),
                CreatedAt = _clock.UtcNow
            },
            cancellationToken);

        return result;
    }
}
