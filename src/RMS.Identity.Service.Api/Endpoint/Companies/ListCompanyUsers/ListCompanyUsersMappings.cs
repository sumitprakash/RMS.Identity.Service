using RMS.Identity.Service.Domain.Contracts.CompanyUsers;

namespace RMS.Identity.Service.Api.Endpoint.Companies.ListCompanyUsers;

public static class ListCompanyUsersMappings
{
    public static ListCompanyUsersResponse ToResponse(this ListCompanyUsersCommandResponse response) =>
        new(response.Users.Select(user => user.ToResponse()).ToArray());

    private static UserResponse ToResponse(this CompanyUserResponseItem user) =>
        new(
            user.UserUuid,
            user.Username,
            user.DisplayName,
            user.Roles,
            user.CompanyRole,
            user.Status,
            user.CreatedAt);
}
