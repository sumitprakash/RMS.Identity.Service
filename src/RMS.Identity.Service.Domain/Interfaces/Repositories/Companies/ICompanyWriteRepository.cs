using RMS.Identity.Service.Domain.Contracts.Companies;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;

public interface ICompanyWriteRepository
{
    Task<long> CreateAsync(
        CreateCompanyCommand command,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        UpdateCompanyCommand command,
        CancellationToken cancellationToken);
}
