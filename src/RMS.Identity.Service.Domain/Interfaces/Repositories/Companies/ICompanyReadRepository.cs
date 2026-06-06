using RMS.Identity.Service.Domain.Entities.Companies;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;

public interface ICompanyReadRepository
{
    Task<bool> ExistsByGstinAsync(
        string gstin,
        CancellationToken cancellationToken);

    Task<Company> GetByIdAsync(
        long companyId,
        CancellationToken cancellationToken);
}
