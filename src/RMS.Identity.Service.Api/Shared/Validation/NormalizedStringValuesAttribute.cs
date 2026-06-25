using System.ComponentModel.DataAnnotations;

namespace RMS.Identity.Service.Api.Shared.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NormalizedStringValuesAttribute : ValidationAttribute
{
    private readonly HashSet<string> _allowedValues;

    public NormalizedStringValuesAttribute(params string[] allowedValues)
    {
        _allowedValues = new HashSet<string>(allowedValues, StringComparer.OrdinalIgnoreCase);
    }

    public override bool IsValid(object? value) =>
        value is null
        || value is not string text
        || _allowedValues.Contains(text.Trim());
}
