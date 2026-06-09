using RMS.Identity.Service.Domain.Contracts.Companies;

namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompany;

public static class UpdateCompanyMappings
{
    public static UpdateCompanyCommandRequest ToCommand(
        this UpdateCompanyRequestBody body,
        Guid companyUuid) =>
        new(
            companyUuid,
            body.LegalName,
            body.TradeName,
            body.Gstin,
            body.ContactEmailAddress,
            body.ContactPhoneNumber,
            body.AddressLine1,
            body.AddressLine2,
            body.City,
            body.State,
            body.PostalCode,
            body.Country);

    public static CompanyResponse ToResponse(this UpdateCompanyCommandResponse response) =>
        new(
            response.CompanyUuid,
            response.CompanyCode,
            response.LegalName,
            response.TradeName,
            response.Gstin,
            response.ContactEmailAddress,
            response.ContactPhoneNumber,
            new RegisteredAddress(
                response.AddressLine1,
                response.AddressLine2,
                response.City,
                response.State,
                response.PostalCode,
                response.Country),
            response.Status);
}
