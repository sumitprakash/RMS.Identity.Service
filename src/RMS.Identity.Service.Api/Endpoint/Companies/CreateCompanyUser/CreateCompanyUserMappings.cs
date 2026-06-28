using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

namespace RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;

public static class CreateCompanyUserMappings
{
    public static CreateCompanyUserCommandRequest ToCommand(this CreateCompanyUserRequest request, Guid actorUserUuid) =>
        new(
            request.CompanyUuid,
            request.Body.Username,
            request.Body.DisplayName,
            request.Body.CompanyRole,
            actorUserUuid);

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
