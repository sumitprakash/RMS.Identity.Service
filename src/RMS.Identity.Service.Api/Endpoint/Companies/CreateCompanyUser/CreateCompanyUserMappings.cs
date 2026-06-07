using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

namespace RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;

public static class CreateCompanyUserMappings
{
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
