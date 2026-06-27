using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LoginRequestBody
{
    [Required]
    [StringLength(32, MinimumLength = 8)]
    [RegularExpression("^[A-Za-z0-9]+$")]
    public required string Username { get; init; }

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@#$&=]).+$")]
    public required string Password { get; init; }
}
