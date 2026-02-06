using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Infrastructure.Repositories
{
    public interface ICompanyRepository
    {
        Task<Company?> GetByCompanyUuidAsync(Guid companyUuid);
        Task<Company?> GetByIdAsync(long companyId);
        Task<long> CreateAsync(Company company);
    }
}
