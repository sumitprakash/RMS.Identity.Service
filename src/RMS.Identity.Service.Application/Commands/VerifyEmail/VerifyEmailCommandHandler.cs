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

    public VerifyEmailCommandHandler(
        IEmailVerificationReadRepository emailVerificationReadRepository,
        IEmailVerificationWriteRepository emailVerificationWriteRepository,
        IPasswordHasher passwordHasher,
        ITextHasher textHasher,
        IUserAccountReadRepository userAccountReadRepository,
        IUserAccountWriteRepository userAccountWriteRepository)
    {
        _emailVerificationReadRepository = emailVerificationReadRepository;
        _emailVerificationWriteRepository = emailVerificationWriteRepository;
        _passwordHasher = passwordHasher;
        _textHasher = textHasher;
        _userAccountReadRepository = userAccountReadRepository;
        _userAccountWriteRepository = userAccountWriteRepository;
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
            throw new ApplicationServiceException(ServiceErrorDefinitions.EmailVerification.EmailVerificationNotFound);
        }

        if (token.Consumed)
        {
            throw new ApplicationServiceException(ServiceErrorDefinitions.EmailVerification.EmailVerificationAlreadyUsed);
        }

        if (token.ExpiresAt <= DateTime.UtcNow)
        {
            throw new ApplicationServiceException(ServiceErrorDefinitions.EmailVerification.EmailVerificationExpired);
        }

        var user = await _userAccountReadRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (!user.IsActive || user.IsDeleted)
        {
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

        return new VerifyEmailCommandResponse(true, "Email verified successfully");
    }

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
