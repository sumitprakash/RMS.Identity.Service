using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SignUpRequestBody
{
    [Required]
    [MinLength(10)]
    [EmailAddress]
    [MaxLength(64)]
    public required string EmailAddress { get; init; }

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@#$&=]).+$")]
    public required string Password { get; init; }

    [Required]
    [MinLength(2)]
    [MaxLength(32)]
    public required string FirstName { get; init; }

    [MinLength(2)]
    [MaxLength(64)]
    public string? MiddleName { get; init; }

    [Required]
    [MinLength(2)]
    [MaxLength(32)]
    public required string LastName { get; init; }

    [Required]
    [StringLength(10, MinimumLength = 10)]
    [RegularExpression("^[0-9]{10}$")]
    public required string PhoneNumber { get; init; }
}
