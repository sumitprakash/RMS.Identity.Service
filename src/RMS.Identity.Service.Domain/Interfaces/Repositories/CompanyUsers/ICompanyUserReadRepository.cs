using RMS.Identity.Service.Domain.Entities.Companies;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;

public interface ICompanyUserReadRepository
{
    Task<IReadOnlyCollection<CompanyUserAccount>> ListByCompanyUuidAsync(
        Guid companyUuid,
        CancellationToken cancellationToken);

    Task<CompanyUserAccount?> GetByCompanyAndUserUuidAsync(
        Guid companyUuid,
        Guid userUuid,
        CancellationToken cancellationToken);

    Task<int> CountActiveOwnersAsync(
        Guid companyUuid,
        CancellationToken cancellationToken);
}
