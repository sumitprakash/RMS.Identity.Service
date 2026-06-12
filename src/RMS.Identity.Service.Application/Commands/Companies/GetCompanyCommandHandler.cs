using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class GetCompanyCommandHandler : ICommandHandler<GetCompanyCommandRequest, GetCompanyCommandResponse>
{
    private readonly ICompanyReadRepository _companyReadRepository;

    public GetCompanyCommandHandler(ICompanyReadRepository companyReadRepository)
    {
        _companyReadRepository = companyReadRepository;
    }

    public async Task<GetCompanyCommandResponse> HandleAsync(
        GetCompanyCommandRequest command,
        CancellationToken cancellationToken)
    {
        var company = await _companyReadRepository.GetByUuidAsync(command.CompanyUuid, cancellationToken);
        return new GetCompanyCommandResponse(
            company.CompanyUuid,
            CompanyCode: null,
            company.LegalName,
            company.TradeName,
            company.Gstin,
            company.ContactEmailAddress,
            company.ContactPhoneNumber,
            company.AddressLine1,
            company.AddressLine2,
            company.City,
            company.State,
            company.PostalCode,
            company.Country,
            company.Status);
    }
}
