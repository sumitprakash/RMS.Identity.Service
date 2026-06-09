using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompanyUser;

public static class UpdateCompanyUserMappings
{
    public static UserResponse ToResponse(this UpdateCompanyUserCommandResponse response) =>
        new(
            response.UserUuid,
            response.Username,
            response.DisplayName,
            response.Roles,
            response.CompanyRole,
            response.Status,
            response.CreatedAt);
}
