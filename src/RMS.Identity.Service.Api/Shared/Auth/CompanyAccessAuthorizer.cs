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
            throw new ApplicationServiceException(ServiceErrorDefinitions.Auth.UserNotActive);
        }

        var membership = await _companyMembershipReadRepository.GetMembershipAsync(
            userUuid,
            companyUuid,
            cancellationToken);

        if (membership is null || !string.Equals(membership.MembershipStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationServiceException(ServiceErrorDefinitions.Auth.CompanyAccessDenied);
        }

        if (!CanAccessCompany(membership.CompanyStatus))
        {
            throw new ApplicationServiceException(ServiceErrorDefinitions.Auth.CompanyAccessDenied);
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
            throw new ApplicationServiceException(ServiceErrorDefinitions.Auth.CompanyRoleRequired);
        }

        return membership;
    }

    private static bool CanAccessCompany(string companyStatus) =>
        string.Equals(companyStatus, "pending_verification", StringComparison.OrdinalIgnoreCase)
        || string.Equals(companyStatus, "verified", StringComparison.OrdinalIgnoreCase);
}
