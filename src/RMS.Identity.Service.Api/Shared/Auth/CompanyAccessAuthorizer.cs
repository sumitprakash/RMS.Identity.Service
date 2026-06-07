using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;

namespace RMS.Identity.Service.Api.Shared.Auth;

public sealed class CompanyAccessAuthorizer : ICompanyAccessAuthorizer
{
    private readonly ICompanyMembershipReadRepository _companyMembershipReadRepository;
    private readonly IUserAccountReadRepository _userAccountReadRepository;

    public CompanyAccessAuthorizer(
        ICompanyMembershipReadRepository companyMembershipReadRepository,
        IUserAccountReadRepository userAccountReadRepository)
    {
        _companyMembershipReadRepository = companyMembershipReadRepository;
        _userAccountReadRepository = userAccountReadRepository;
    }

    public async Task<CompanyMembership> AuthorizeMembershipAsync(
        Guid userUuid,
        Guid companyUuid,
        CancellationToken cancellationToken)
    {
        var user = await _userAccountReadRepository.GetByUuidAsync(userUuid, cancellationToken);
        if (!user.IsActive || user.IsDeleted)
        {
            throw new ServiceException(
                StatusCodes.Status403Forbidden,
                "USER_NOT_ACTIVE",
                "User is not allowed to access this company.");
        }

        var membership = await _companyMembershipReadRepository.GetMembershipAsync(
            userUuid,
            companyUuid,
            cancellationToken);

        if (membership is null || !string.Equals(membership.MembershipStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            throw new ServiceException(
                StatusCodes.Status403Forbidden,
                "COMPANY_ACCESS_DENIED",
                "User does not have active access to this company.");
        }

        return membership;
    }

    public async Task<CompanyMembership> AuthorizeRoleAsync(
        Guid userUuid,
        Guid companyUuid,
        IReadOnlyCollection<string> allowedRoles,
        CancellationToken cancellationToken)
    {
        var membership = await AuthorizeMembershipAsync(userUuid, companyUuid, cancellationToken);
        if (!allowedRoles.Contains(membership.CompanyRole, StringComparer.OrdinalIgnoreCase))
        {
            throw new ServiceException(
                StatusCodes.Status403Forbidden,
                "COMPANY_ROLE_REQUIRED",
                "User does not have the required company role.");
        }

        return membership;
    }
}
