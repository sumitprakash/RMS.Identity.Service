using RMS.Identity.Service.Domain.Contracts.VerifyEmail;

namespace RMS.Identity.Service.Api.Endpoint.Users.VerifyEmail;

public static class VerifyEmailMappings
{
    public static VerifyEmailCommandRequest ToCommand(this VerifyEmailRequestBody body) =>
        new(body.Token);

    public static VerifyEmailResponse ToResponse(this VerifyEmailCommandResponse response) =>
        new(response.Success, response.Message);
}
