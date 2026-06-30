using System.ComponentModel.DataAnnotations;

namespace RMS.Identity.Service.Api.Shared.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotBlankAttribute : ValidationAttribute
{
    public NotBlankAttribute()
    {
        ErrorMessage = "The {0} field must not be blank.";
    }

    public override bool IsValid(object? value) =>
        value is null
        || value is not string stringValue
        || !string.IsNullOrWhiteSpace(stringValue);
}
