using RMS.Identity.Service.Domain.Contracts.Companies;

namespace RMS.Identity.Service.Api.Endpoint.Companies;

public static class RegisterCompanyMappings
{
    public static RegisterCompanyCommandRequest ToCommand(this RegisterCompanyRequest request, Guid ownerUserUuid) =>
        new(
            ownerUserUuid,
            request.Body.LegalName,
            request.Body.TradeName,
            request.Body.Gstin,
            request.Body.ContactEmailAddress,
            request.Body.ContactPhoneNumber,
            request.Body.AddressLine1,
            request.Body.AddressLine2,
            request.Body.City,
            request.Body.State,
            request.Body.PostalCode,
            request.Body.Country);

    public static RegisterCompanyResponse ToResponse(this RegisterCompanyCommandResponse response) =>
        new(
            response.CompanyUuid,
            response.LegalName,
            response.TradeName,
            response.Gstin,
            response.Status,
            response.CreatedAt);

    public static MyCompaniesResponse ToResponse(this GetMyCompaniesCommandResponse response) =>
        new(
            response.Companies
                .Select(company => new MyCompanyResponse(
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
