using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Entities.SignUp;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public static class SignUpMappings
{
    public static SignUpCommand ToCommand(this SignUpRequest request, string? idempotencyKey) =>
        new(
            request.Username,
            request.Password,
            request.DisplayName,
            request.Phone,
            idempotencyKey);

    public static SignUpResponse ToResponse(this SignUpUser user) =>
        new(
            user.UserUuid,
            user.Username,
            user.DisplayName,
            user.Status,
            user.CreatedAt);
}
