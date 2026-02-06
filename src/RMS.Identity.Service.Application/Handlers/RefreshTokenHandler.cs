using MediatR;
using RMS.Identity.Service.Application.Commands;
using RMS.Identity.Service.Application.DTOs;
using RMS.Identity.Service.Application.Repositories;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace RMS.Identity.Service.Application.Handlers
{
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
    {
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly IUserRepository _userRepo;
        private readonly IUserRoleRepository _userRoleRepo;
        private readonly ITokenGenerator _tokenGenerator;

        // Note: repositories use DbExecutor internally and rely on IUnitOfWork for transaction context
        public RefreshTokenHandler(
            IRefreshTokenRepository refreshRepo,
            IUserRepository userRepo,
            IUserRoleRepository userRoleRepo,
            ITokenGenerator tokenGenerator)
        {
            _refreshRepo = refreshRepo;
            _userRepo = userRepo;
            _userRoleRepo = userRoleRepo;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // We are in a transactional handler because command implements ITransactionalCommand.
            // All repository write calls below will participate in the UnitOfWork transaction
            // started by TransactionBehavior.

            var hashed = ComputeHash(request.RawRefreshToken);
            var existing = await _refreshRepo.GetByHashAsync(hashed);
            if (existing == null || existing.RevokedAt != null)
                throw new SecurityException("Invalid refresh token");

            // revoke old token (write)
            await _refreshRepo.RevokeAsync(existing.RefreshTokenID);

            // create new token (write)
            var newToken = _refreshRepo.CreateForUser(existing.UserID); // implement factory in Domain
            await _refreshRepo.SaveAsync(newToken);

            // create access token (reads)
            var user = await _userRepo.GetByIdAsync(existing.UserID)
                        ?? throw new InvalidOperationException("User not found");

            var roles = (await _userRoleRepo.GetRolesForUserAsync(user.UserID)).Select(r => r.Name!).ToArray();

            var access = _tokenGenerator.CreateAccessToken(user.UserUUID, user.CompanyID, roles);

            return new AuthResult { AccessToken = access, RefreshToken = newToken.TokenHash };
        }

        private static string ComputeHash(string raw)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }
}