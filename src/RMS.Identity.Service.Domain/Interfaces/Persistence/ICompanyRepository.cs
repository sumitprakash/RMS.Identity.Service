using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(long companyId, CancellationToken cancellationToken = default);

    Task<Company?> GetByUuidAsync(Guid companyUuid, CancellationToken cancellationToken = default);
}
