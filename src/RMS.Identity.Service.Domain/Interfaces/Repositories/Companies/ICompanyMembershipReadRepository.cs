using RMS.Identity.Service.Domain.Entities.Companies;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;

public interface ICompanyMembershipReadRepository
{
    Task<IReadOnlyCollection<UserCompanyMembership>> ListByUserUuidAsync(
        Guid userUuid,
        CancellationToken cancellationToken);

    Task<CompanyMembership?> GetMembershipAsync(
        Guid userUuid,
        Guid companyUuid,
        CancellationToken cancellationToken);
}
