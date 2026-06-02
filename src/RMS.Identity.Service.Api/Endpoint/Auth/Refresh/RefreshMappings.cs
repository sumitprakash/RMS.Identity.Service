using RMS.Identity.Service.Domain.Contracts.Refresh;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

public static class RefreshMappings
{
    public static RefreshCommandRequest ToCommand(this RefreshRequest request) =>
        new(request.Body.RefreshToken);

    public static RefreshResponse ToResponse(this RefreshCommandResponse response) =>
        new(
            response.AccessToken,
            response.RefreshToken,
            response.ExpiresIn);
}
