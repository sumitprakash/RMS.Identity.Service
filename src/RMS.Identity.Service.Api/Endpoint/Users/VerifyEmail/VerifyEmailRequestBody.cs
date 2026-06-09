namespace RMS.Identity.Service.Api.Endpoint.Users.VerifyEmail;

public sealed class VerifyEmailRequestBody
{
    public string Token { get; init; } = string.Empty;
}
