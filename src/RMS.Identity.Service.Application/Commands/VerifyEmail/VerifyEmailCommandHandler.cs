using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler : ICommandHandler<VerifyEmailCommandRequest, VerifyEmailCommandResponse>
{
    private const string EmailVerificationPurpose = "email_verification";

    private readonly IEmailVerificationReadRepository _emailVerificationReadRepository;
    private readonly IEmailVerificationWriteRepository _emailVerificationWriteRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITextHasher _textHasher;
    private readonly IUserAccountReadRepository _userAccountReadRepository;
    private readonly IUserAccountWriteRepository _userAccountWriteRepository;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IEmailVerificationReadRepository emailVerificationReadRepository,
        IEmailVerificationWriteRepository emailVerificationWriteRepository,
        IPasswordHasher passwordHasher,
        ITextHasher textHasher,
        IUserAccountReadRepository userAccountReadRepository,
        IUserAccountWriteRepository userAccountWriteRepository,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _emailVerificationReadRepository = emailVerificationReadRepository;
        _emailVerificationWriteRepository = emailVerificationWriteRepository;
        _passwordHasher = passwordHasher;
        _textHasher = textHasher;
        _userAccountReadRepository = userAccountReadRepository;
        _userAccountWriteRepository = userAccountWriteRepository;
        _logger = logger;
    }

    public async Task<VerifyEmailCommandResponse> HandleAsync(
        VerifyEmailCommandRequest command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            throw ValidationError("Verification token is required.");
        }

        if (command.Token.Length > 256)
        {
            throw ValidationError("Verification token must not exceed 256 characters.");
        }

        if ((command.Password?.Length ?? 0) > 128)
        {
            throw ValidationError("Password must not exceed 128 characters.");
        }

        var tokenHash = _textHasher.Hash(command.Token.Trim());
        var token = await _emailVerificationReadRepository.GetByTokenHashAsync(
            tokenHash,
            EmailVerificationPurpose,
            cancellationToken);

        if (token is null)
        {
            _logger.LogWarning("Email verification rejected because the token was not found.");
            throw new ApplicationServiceException(ServiceErrorDefinitions.EmailVerification.EmailVerificationNotFound);
        }

        if (token.Consumed)
        {
            _logger.LogWarning("Email verification rejected because token {EmailVerificationId} was already consumed.", token.EmailVerificationId);
            throw new ApplicationServiceException(ServiceErrorDefinitions.EmailVerification.EmailVerificationAlreadyUsed);
        }

        if (token.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Email verification rejected because token {EmailVerificationId} expired.", token.EmailVerificationId);
            throw new ApplicationServiceException(ServiceErrorDefinitions.EmailVerification.EmailVerificationExpired);
        }

        var user = await _userAccountReadRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (!user.IsActive || user.IsDeleted)
        {
            _logger.LogWarning("Email verification rejected because user {UserUuid} is inactive.", user.UserUuid);
            throw new ApplicationServiceException(ServiceErrorDefinitions.Auth.UserNotActive);
        }

        if (user.PasswordSetupRequired
            && (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 8))
        {
            throw ValidationError("Password must be at least 8 characters long to activate this account.");
        }

        var consumed = await _emailVerificationWriteRepository.TryConsumeAsync(
            token.EmailVerificationId,
            cancellationToken);
        if (!consumed)
        {
            _logger.LogWarning("Email verification token {EmailVerificationId} was already consumed during verification.", token.EmailVerificationId);
            throw new ApplicationServiceException(ServiceErrorDefinitions.EmailVerification.EmailVerificationAlreadyUsed);
        }

        if (user.PasswordSetupRequired)
        {
            await _userAccountWriteRepository.CompletePasswordSetupAsync(
                user.UserId,
                _passwordHasher.Hash(command.Password!),
                cancellationToken);
        }
        else
        {
            await _userAccountWriteRepository.MarkEmailVerifiedAsync(user.UserId, cancellationToken);
        }
        _logger.LogInformation("Email verified for user {UserUuid}.", user.UserUuid);

        return new VerifyEmailCommandResponse(true, "Email verified successfully");
    }

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
