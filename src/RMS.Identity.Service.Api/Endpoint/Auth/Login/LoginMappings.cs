using RMS.Identity.Service.Domain.Contracts.Login;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

public static class LoginMappings
{
    public static LoginCommandRequest ToCommand(this LoginRequest request) =>
        new(
            request.Body.Username,
            request.Body.Password);

    public static LoginResponse ToResponse(this LoginCommandResponse response) =>
        new(
            response.AccessToken,
            response.RefreshToken,
            response.ExpiresIn,
            response.TokenType,
            new LoginUserResponse(
                response.User.UserUuid,
                response.User.Username,
                response.User.DisplayName,
                response.User.Roles,
                response.User.Status,
                response.User.CreatedAt));
}
