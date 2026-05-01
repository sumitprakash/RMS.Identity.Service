using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Entities.SignUp;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public static class SignUpMappings
{
    public static SignUpCommand ToCommand(this SignUpRequest request) =>
        new(
            request.Body.EmailAddress,
            request.Body.Password,
            request.Body.FirstName,
            request.Body.MiddleName,
            request.Body.LastName,
            request.Body.PhoneNumber,
            request.IdempotencyKey);

    public static SignUpResponse ToResponse(this SignUpUser user) =>
        new(
            user.UserUuid,
            user.Username,
            user.Status,
            user.CreatedAt);
}
