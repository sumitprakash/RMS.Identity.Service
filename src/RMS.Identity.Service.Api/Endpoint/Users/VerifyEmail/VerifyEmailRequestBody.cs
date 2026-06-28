using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.Users.VerifyEmail;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class VerifyEmailRequestBody
{
    [Required]
    [MinLength(32)]
    [MaxLength(256)]
    public required string Token { get; init; }

    [MinLength(8)]
    [MaxLength(128)]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@#$&=]).+$")]
    public string? Password { get; init; }
}
