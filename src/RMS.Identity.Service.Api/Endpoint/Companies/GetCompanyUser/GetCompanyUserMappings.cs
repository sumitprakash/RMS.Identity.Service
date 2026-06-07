using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

namespace RMS.Identity.Service.Api.Endpoint.Companies.GetCompanyUser;

public static class GetCompanyUserMappings
{
    public static UserResponse ToResponse(this GetCompanyUserCommandResponse response) =>
        new(
            response.UserUuid,
            response.Username,
            response.DisplayName,
            response.Roles,
            response.CompanyRole,
            response.Status,
            response.CreatedAt);
}
