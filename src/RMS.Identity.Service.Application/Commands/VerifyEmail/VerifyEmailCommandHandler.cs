using System.Net;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Application.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler : ICommandHandler<VerifyEmailCommandRequest, VerifyEmailCommandResponse>
{
    private const string EmailVerificationPurpose = "email_verification";

    private readonly IEmailVerificationReadRepository _emailVerificationReadRepository;
    private readonly IEmailVerificationWriteRepository _emailVerificationWriteRepository;
    private readonly ITextHasher _textHasher;
    private readonly IUserAccountReadRepository _userAccountReadRepository;
    private readonly IUserAccountWriteRepository _userAccountWriteRepository;

    public VerifyEmailCommandHandler(
        IEmailVerificationReadRepository emailVerificationReadRepository,
        IEmailVerificationWriteRepository emailVerificationWriteRepository,
        ITextHasher textHasher,
        IUserAccountReadRepository userAccountReadRepository,
        IUserAccountWriteRepository userAccountWriteRepository)
    {
        _emailVerificationReadRepository = emailVerificationReadRepository;
        _emailVerificationWriteRepository = emailVerificationWriteRepository;
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

        var tokenHash = _textHasher.Hash(command.Token.Trim());
        var token = await _emailVerificationReadRepository.GetByTokenHashAsync(
            tokenHash,
            EmailVerificationPurpose,
            cancellationToken);

        if (token is null)
        {
            throw new ServiceException(
                (int)HttpStatusCode.NotFound,
                "EMAIL_VERIFICATION_NOT_FOUND",
                "Email verification token could not be found.");
        }

        if (token.Consumed)
        {
            throw ValidationError("Email verification token has already been used.");
        }

        if (token.ExpiresAt <= DateTime.UtcNow)
        {
            throw ValidationError("Email verification token has expired.");
        }

        var user = await _userAccountReadRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (!user.IsActive || user.IsDeleted)
        {
            throw new ServiceException(
                (int)HttpStatusCode.Forbidden,
                "USER_NOT_ACTIVE",
                "User account is not active.");
        }

        await _userAccountWriteRepository.MarkEmailVerifiedAsync(user.UserId, cancellationToken);
        await _emailVerificationWriteRepository.ConsumeAsync(token.EmailVerificationId, cancellationToken);

        return new VerifyEmailCommandResponse(true, "Email verified successfully");
    }

    private static ServiceException ValidationError(string message) =>
        new((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", message);
}
