using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class GetMyCompaniesCommandHandler : ICommandHandler<GetMyCompaniesCommandRequest, GetMyCompaniesCommandResponse>
{
    private readonly ICompanyMembershipReadRepository _companyMembershipReadRepository;

    public GetMyCompaniesCommandHandler(ICompanyMembershipReadRepository companyMembershipReadRepository)
    {
        _companyMembershipReadRepository = companyMembershipReadRepository;
    }

    public async Task<GetMyCompaniesCommandResponse> HandleAsync(
        GetMyCompaniesCommandRequest command,
        CancellationToken cancellationToken)
    {
        var companies = await _companyMembershipReadRepository.ListByUserUuidAsync(command.UserUuid, cancellationToken);
        return new GetMyCompaniesCommandResponse(
            companies
                .Select(company => new UserCompanyCommandResponse(
                    company.CompanyUuid,
                    company.LegalName,
                    company.TradeName,
                    company.Gstin,
                    company.Status,
                    company.CompanyRole,
                    company.MembershipStatus,
                    company.CreatedAt))
                .ToArray());
    }
}
