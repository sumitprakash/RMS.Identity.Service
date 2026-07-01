using System.ComponentModel.DataAnnotations;

namespace RMS.Identity.Service.Api.Shared.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotEmptyOrWhiteSpaceAttribute : ValidationAttribute
{
    public NotEmptyOrWhiteSpaceAttribute()
    {
        ErrorMessage = "The {0} field must not be empty or whitespace.";
    }

    public override bool IsValid(object? value) =>
        value is null
        || value is not string stringValue
        || !string.IsNullOrWhiteSpace(stringValue);
}
