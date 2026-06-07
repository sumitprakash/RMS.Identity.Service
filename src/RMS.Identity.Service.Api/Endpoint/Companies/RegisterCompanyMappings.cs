using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

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

    public static CreateCompanyUserCommandRequest ToCommand(
        this CreateCompanyUserRequest request,
        Guid companyUuid) =>
        new(
            companyUuid,
            request.Body.Username,
            request.Body.DisplayName,
            request.Body.CompanyRole);

    public static UserResponse ToResponse(this CreateCompanyUserCommandResponse response) =>
        new(
            response.UserUuid,
            response.Username,
            response.DisplayName,
            response.Roles,
            response.CompanyRole,
            response.Status,
            response.CreatedAt);
}
