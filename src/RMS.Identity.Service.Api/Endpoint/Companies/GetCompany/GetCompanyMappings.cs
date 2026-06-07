using RMS.Identity.Service.Domain.Contracts.Companies;

namespace RMS.Identity.Service.Api.Endpoint.Companies.GetCompany;

public static class GetCompanyMappings
{
    public static CompanyResponse ToResponse(this GetCompanyCommandResponse response) =>
        new(
            response.CompanyUuid,
            response.CompanyCode,
            response.LegalName,
            response.TradeName,
            response.Gstin,
            response.ContactEmailAddress,
            response.ContactPhoneNumber,
            new RegisteredAddressResponse(
                response.AddressLine1,
                response.AddressLine2,
                response.City,
                response.State,
                response.PostalCode,
                response.Country),
            response.Status);
}
