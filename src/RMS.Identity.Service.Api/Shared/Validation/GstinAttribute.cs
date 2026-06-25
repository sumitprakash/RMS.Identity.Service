using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace RMS.Identity.Service.Api.Shared.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class GstinAttribute : ValidationAttribute
{
    private static readonly Regex Validator = new(
        "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    public override bool IsValid(object? value) =>
        value is null
        || value is not string text
        || Validator.IsMatch(text.Trim());
}
