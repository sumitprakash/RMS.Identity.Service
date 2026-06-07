using RMS.Identity.Service.Domain.Entities.Companies;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;

public interface ICompanyUserReadRepository
{
    Task<CompanyUserAccount?> GetByCompanyAndUserUuidAsync(
        Guid companyUuid,
        Guid userUuid,
        CancellationToken cancellationToken);
}
