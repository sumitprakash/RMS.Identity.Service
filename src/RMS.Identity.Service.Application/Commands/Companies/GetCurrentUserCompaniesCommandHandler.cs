using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class GetCurrentUserCompaniesCommandHandler : ICommandHandler<GetCurrentUserCompaniesCommandRequest, GetCurrentUserCompaniesCommandResponse>
{
    private readonly ICompanyMembershipReadRepository _companyMembershipReadRepository;

    public GetCurrentUserCompaniesCommandHandler(ICompanyMembershipReadRepository companyMembershipReadRepository)
    {
        _companyMembershipReadRepository = companyMembershipReadRepository;
    }

    public async Task<GetCurrentUserCompaniesCommandResponse> HandleAsync(
        GetCurrentUserCompaniesCommandRequest command,
        CancellationToken cancellationToken)
    {
        var companies = await _companyMembershipReadRepository.ListByUserUuidAsync(command.UserUuid, cancellationToken);
        return new GetCurrentUserCompaniesCommandResponse(
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
