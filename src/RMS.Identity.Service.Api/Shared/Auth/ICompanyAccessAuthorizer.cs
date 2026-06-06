using RMS.Identity.Service.Domain.Entities.Companies;

namespace RMS.Identity.Service.Api.Shared.Auth;

public interface ICompanyAccessAuthorizer
{
    Task<CompanyMembership> AuthorizeMembershipAsync(
        Guid userUuid,
        Guid companyUuid,
        CancellationToken cancellationToken);

    Task<CompanyMembership> AuthorizeRoleAsync(
        Guid userUuid,
        Guid companyUuid,
        IReadOnlyCollection<string> allowedRoles,
        CancellationToken cancellationToken);
}
