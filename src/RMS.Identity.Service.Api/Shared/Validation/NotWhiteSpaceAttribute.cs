using System.ComponentModel.DataAnnotations;

namespace RMS.Identity.Service.Api.Shared.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is null || value is not string text || !string.IsNullOrWhiteSpace(text);
}
