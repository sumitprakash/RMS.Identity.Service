using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Roles;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;

namespace RMS.Identity.Service.Api.Shared.Auth;

public sealed class PlatformAdminAuthorizer : IPlatformAdminAuthorizer
{
    private static readonly string[] PlatformAdminRoles = ["PLATFORM_ADMIN"];

    private readonly IOperationalRoleReadRepository _operationalRoleReadRepository;
    private readonly IUserAccountReadRepository _userAccountReadRepository;

    public PlatformAdminAuthorizer(
        IOperationalRoleReadRepository operationalRoleReadRepository,
        IUserAccountReadRepository userAccountReadRepository)
    {
        _operationalRoleReadRepository = operationalRoleReadRepository;
        _userAccountReadRepository = userAccountReadRepository;
    }

    public async Task AuthorizeAsync(
        Guid userUuid,
        CancellationToken cancellationToken)
    {
        var user = await _userAccountReadRepository.GetByUuidAsync(userUuid, cancellationToken);
        if (!user.IsActive || user.IsDeleted)
        {
            throw new ForbiddenException("User is not allowed to perform platform administration.");
        }

        if (!await _operationalRoleReadRepository.UserHasAnyRoleAsync(userUuid, PlatformAdminRoles, cancellationToken))
        {
            throw new ForbiddenException("User must be a platform admin.");
        }
    }
}
