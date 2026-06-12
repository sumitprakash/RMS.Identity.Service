using System.Net;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.Login;
using RMS.Identity.Service.Domain.Entities.Auth;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Auth;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommandRequest, LoginCommandResponse>
{
    private readonly IAuthenticationRepository _authenticationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenGenerator _authTokenGenerator;
    private readonly ITextHasher _textHasher;

    public LoginCommandHandler(
        IAuthenticationRepository authenticationRepository,
        IPasswordHasher passwordHasher,
        IAuthTokenGenerator authTokenGenerator,
        ITextHasher textHasher)
    {
        _authenticationRepository = authenticationRepository;
        _passwordHasher = passwordHasher;
        _authTokenGenerator = authTokenGenerator;
        _textHasher = textHasher;
    }

    public async Task<LoginCommandResponse> HandleAsync(LoginCommandRequest command, CancellationToken cancellationToken)
    {
        var username = EmailAddressValidator.Normalize(command.Username);
        var user = await _authenticationRepository.GetByUsernameAsync(username, cancellationToken);

        if (user is null)
        {
            throw InvalidCredentials();
        }

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            await _authenticationRepository.RecordFailedLoginAsync(user.UserId, cancellationToken);
            throw InvalidCredentials();
        }

        EnsureUserCanLogin(user);

        var tokens = _authTokenGenerator.Generate(user);
        await _authenticationRepository.RecordSuccessfulLoginAsync(
            user.UserId,
            _textHasher.Hash(tokens.RefreshToken),
            tokens.RefreshTokenExpiresAt,
            cancellationToken);

        return new LoginCommandResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresIn,
            "Bearer",
            new LoginUserCommandResponse(
                user.UserUuid,
                user.Username,
                user.DisplayName,
                user.Roles,
                user.Status,
                user.CreatedAt));
    }

    private static void EnsureUserCanLogin(AuthenticatedUser user)
    {
        if (user.IsDeleted || !user.IsActive)
        {
            throw Forbidden("ACCOUNT_INACTIVE", "User account is inactive.");
        }

        if (user.LockedUntil is not null && user.LockedUntil > DateTime.UtcNow)
        {
            throw Forbidden("ACCOUNT_LOCKED", "User account is temporarily locked.");
        }

        if (!user.EmailVerified)
        {
            throw Forbidden("EMAIL_NOT_VERIFIED", "Email address is not verified.");
        }

    }

    private static ServiceException InvalidCredentials() =>
        new((int)HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS", "Username or password is incorrect.");

    private static ServiceException Forbidden(string code, string message) =>
        new((int)HttpStatusCode.Forbidden, code, message);
}
