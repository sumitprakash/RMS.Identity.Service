using RMS.Identity.Service.Domain.Contracts.Companies;

namespace RMS.Identity.Service.Api.Endpoint.Companies.GetCurrentUserCompanies;

public static class GetCurrentUserCompaniesMappings
{
    public static CurrentUserCompaniesResponse ToResponse(this GetCurrentUserCompaniesCommandResponse response) =>
        new(
            response.Companies
                .Select(company => new CurrentUserCompanyResponse(
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
