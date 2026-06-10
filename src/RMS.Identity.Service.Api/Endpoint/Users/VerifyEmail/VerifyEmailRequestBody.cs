using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.Users.VerifyEmail;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class VerifyEmailRequestBody
{
    public string Token { get; init; } = string.Empty;
}
