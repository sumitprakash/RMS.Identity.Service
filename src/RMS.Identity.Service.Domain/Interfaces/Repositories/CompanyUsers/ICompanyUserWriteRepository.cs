using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

namespace RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;

public interface ICompanyUserWriteRepository
{
    Task CreateAsync(
        CreateCompanyUserCommand command,
        CancellationToken cancellationToken);

    Task<int> CountActiveOwnersForUpdateAsync(
        Guid companyUuid,
        CancellationToken cancellationToken);

    Task UpdateMembershipAsync(
        UpdateCompanyUserCommand command,
        CancellationToken cancellationToken);
}
