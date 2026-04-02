using RMS.Identity.Service.Api.Contracts;
using RMS.Identity.Service.Application.Identity.Results;

namespace RMS.Identity.Service.Api.Extensions;

internal static class ResultMappings
{
    public static UserResponse ToResponse(this UserResult result)
    {
        return new UserResponse(
            result.UserUuid,
            result.Username,
            result.DisplayName,
            result.Roles,
            result.Status,
            result.CreatedAt);
    }

    public static CompanyResponse ToResponse(this CompanyResult result)
    {
        return new CompanyResponse(
            result.CompanyUuid,
            result.CompanyCode,
            result.CompanyName,
            result.CompanyGstin);
    }

    public static LoginResponse ToResponse(this LoginResult result)
    {
        return new LoginResponse(
            result.AccessToken,
            result.RefreshToken,
            result.ExpiresIn,
            result.TokenType,
            result.User.ToResponse());
    }

    public static RefreshResponse ToResponse(this RefreshTokenResult result)
    {
        return new RefreshResponse(
            result.AccessToken,
            result.RefreshToken,
            result.ExpiresIn);
    }
}
