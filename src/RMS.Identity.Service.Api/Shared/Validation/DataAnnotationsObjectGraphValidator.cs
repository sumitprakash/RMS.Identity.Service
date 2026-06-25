using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Shared.Validation;

internal static class DataAnnotationsObjectGraphValidator
{
    public static void Validate(object instance)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Validate(instance, visited);
    }

    private static void Validate(object instance, HashSet<object> visited)
    {
        if (!visited.Add(instance))
        {
            return;
        }

        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                instance,
                new ValidationContext(instance),
                validationResults,
                validateAllProperties: true))
        {
            var message = validationResults
                .Select(result => result.ErrorMessage)
                .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
                ?? "Request validation failed.";

            throw new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
        }

        foreach (var property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var value = property.GetValue(instance);
            if (value is null || IsSimple(value.GetType()))
            {
                continue;
            }

            if (value is IEnumerable collection)
            {
                foreach (var item in collection)
                {
                    if (item is not null && !IsSimple(item.GetType()))
                    {
                        Validate(item, visited);
                    }
                }

                continue;
            }

            Validate(value, visited);
        }
    }

    private static bool IsSimple(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType.IsPrimitive
            || underlyingType.IsEnum
            || underlyingType == typeof(string)
            || underlyingType == typeof(decimal)
            || underlyingType == typeof(DateTime)
            || underlyingType == typeof(DateTimeOffset)
            || underlyingType == typeof(TimeSpan)
            || underlyingType == typeof(Guid);
    }
}
