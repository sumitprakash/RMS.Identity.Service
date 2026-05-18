using RMS.Identity.Service.Domain.Contracts.SignUp;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public static class SignUpMappings
{
    public static SignUpCommandRequest ToCommand(this SignUpRequest request) =>
        request.Body.ToCommand();

    public static SignUpCommandRequest ToCommand(this SignUpRequestBody request) =>
        new(
            request.EmailAddress,
            request.Password,
            request.FirstName,
            request.MiddleName,
            request.LastName,
            request.PhoneNumber);

    public static SignUpResponse ToResponse(this SignUpCommandResponse user) =>
        new(
            user.UserUuid,
            user.Username,
            user.Status,
            user.CreatedAt);
}
