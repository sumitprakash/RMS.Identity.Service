using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class GetCurrentUserCompaniesCommandHandler : ICommandHandler<GetCurrentUserCompaniesCommandRequest, GetCurrentUserCompaniesCommandResponse>
{
    private readonly ICompanyMembershipReadRepository _companyMembershipReadRepository;
    private readonly IUserAccountReadRepository _userAccountReadRepository;

    public GetCurrentUserCompaniesCommandHandler(
        ICompanyMembershipReadRepository companyMembershipReadRepository,
        IUserAccountReadRepository userAccountReadRepository)
    {
        _companyMembershipReadRepository = companyMembershipReadRepository;
        _userAccountReadRepository = userAccountReadRepository;
    }

    public async Task<GetCurrentUserCompaniesCommandResponse> HandleAsync(
        GetCurrentUserCompaniesCommandRequest command,
        CancellationToken cancellationToken)
    {
        var user = await _userAccountReadRepository.GetByUuidAsync(command.UserUuid, cancellationToken);
        if (!user.IsActive || user.IsDeleted)
        {
            throw new ApplicationServiceException(ServiceErrorDefinitions.Auth.UserNotActive);
        }

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
