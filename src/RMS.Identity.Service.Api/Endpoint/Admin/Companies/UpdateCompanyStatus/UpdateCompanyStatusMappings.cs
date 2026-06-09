using RMS.Identity.Service.Domain.Contracts.Companies;

namespace RMS.Identity.Service.Api.Endpoint.Admin.Companies.UpdateCompanyStatus;

public static class UpdateCompanyStatusMappings
{
    public static CompanyResponse ToResponse(this UpdateCompanyStatusCommandResponse response) =>
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
