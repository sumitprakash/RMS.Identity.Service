using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Refresh;
using RMS.Identity.Service.Domain.Entities.Auth;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Auth;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Refresh;

public sealed class RefreshCommandHandler : ICommandHandler<RefreshCommandRequest, RefreshCommandResponse>
{
    private readonly IAuthenticationRepository _authenticationRepository;
    private readonly IAuthTokenGenerator _authTokenGenerator;
    private readonly ITextHasher _textHasher;
    private readonly ILogger<RefreshCommandHandler> _logger;

    public RefreshCommandHandler(
        IAuthenticationRepository authenticationRepository,
        IAuthTokenGenerator authTokenGenerator,
        ITextHasher textHasher,
        ILogger<RefreshCommandHandler> logger)
    {
        _authenticationRepository = authenticationRepository;
        _authTokenGenerator = authTokenGenerator;
        _textHasher = textHasher;
        _logger = logger;
    }

    public async Task<RefreshCommandResponse> HandleAsync(RefreshCommandRequest command, CancellationToken cancellationToken)
    {
        var currentRefreshTokenHash = _textHasher.Hash(command.RefreshToken);
        var session = await _authenticationRepository.GetRefreshTokenSessionAsync(
            currentRefreshTokenHash,
            cancellationToken);

        if (session is null || !CanUseRefreshToken(session))
        {
            _logger.LogWarning("Refresh token rotation rejected because the refresh token session is missing or unusable.");
            throw InvalidRefreshToken();
        }

        EnsureUserCanRefresh(session.User);

        var tokens = _authTokenGenerator.Generate(session.User);
        var newRefreshTokenHash = _textHasher.Hash(tokens.RefreshToken);
        var rotated = await _authenticationRepository.RotateRefreshTokenAsync(
            session.RefreshTokenId,
            session.User.UserId,
            newRefreshTokenHash,
            tokens.RefreshTokenExpiresAt,
            cancellationToken);

        if (!rotated)
        {
            _logger.LogWarning(
                "Refresh token rotation failed for user {UserUuid} because the token was already changed.",
                session.User.UserUuid);
            throw InvalidRefreshToken();
        }
        _logger.LogInformation("Refresh token rotated for user {UserUuid}.", session.User.UserUuid);

        return new RefreshCommandResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresIn);
    }

    private static bool CanUseRefreshToken(RefreshTokenSession session) =>
        session.RevokedAt is null && session.ExpiresAt > DateTime.UtcNow;

    private static void EnsureUserCanRefresh(AuthenticatedUser user)
    {
        if (user.IsDeleted || !user.IsActive)
        {
            throw InvalidRefreshToken();
        }

        if (user.LockedUntil is not null && user.LockedUntil > DateTime.UtcNow)
        {
            throw InvalidRefreshToken();
        }

        if (!user.EmailVerified)
        {
            throw InvalidRefreshToken();
        }

        if (user.PasswordSetupRequired)
        {
            throw InvalidRefreshToken();
        }
    }

    private static ServiceException InvalidRefreshToken() =>
        new ApplicationServiceException(ServiceErrorDefinitions.Auth.InvalidRefreshToken);
}
