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
            _ = _passwordHasher.Hash(command.Password);
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
            throw Forbidden(ServiceErrorDefinitions.Auth.AccountInactive);
        }

        if (user.PasswordSetupRequired)
        {
            throw Forbidden(ServiceErrorDefinitions.Auth.PasswordSetupRequired);
        }

        if (user.LockedUntil is not null && user.LockedUntil > DateTime.UtcNow)
        {
            throw Forbidden(ServiceErrorDefinitions.Auth.AccountLocked);
        }

        if (!user.EmailVerified)
        {
            throw Forbidden(ServiceErrorDefinitions.Auth.EmailNotVerified);
        }

    }

    private static ServiceException InvalidCredentials() =>
        new ApplicationServiceException(ServiceErrorDefinitions.Auth.InvalidCredentials);

    private static ServiceException Forbidden(ServiceError error) =>
        new ApplicationServiceException(error);
}
