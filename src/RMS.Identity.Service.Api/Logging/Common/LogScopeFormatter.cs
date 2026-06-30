using System.Text;

namespace RMS.Identity.Service.Api.Logging.Common;

public static class LogScopeFormatter
{
    public const string UnavailableCorrelationTraceId = "unavailable";

    public static IReadOnlyDictionary<string, string?> Capture(IExternalScopeProvider scopeProvider)
    {
        var properties = new Dictionary<string, string?>(StringComparer.Ordinal);
        scopeProvider.ForEachScope(
            static (scope, properties) => AppendScope(scope, properties),
            properties);

        if (!properties.ContainsKey("CorrelationTraceId"))
        {
            properties["CorrelationTraceId"] = UnavailableCorrelationTraceId;
        }

        return properties;
    }

    public static string Format(IReadOnlyDictionary<string, string?> properties)
    {
        var builder = new StringBuilder();
        foreach (var property in properties)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder
                .Append(property.Key)
                .Append('=')
                .Append(property.Value ?? string.Empty);
        }

        return builder.Length == 0 ? string.Empty : $" {builder}";
    }

    private static void AppendScope(object? scope, Dictionary<string, string?> properties)
    {
        if (scope is IEnumerable<KeyValuePair<string, object?>> objectProperties)
        {
            foreach (var property in objectProperties)
            {
                if (string.Equals(property.Key, "{OriginalFormat}", StringComparison.Ordinal))
                {
                    continue;
                }

                properties[property.Key] = FormatValue(property.Value);
            }

            return;
        }

        if (scope is IEnumerable<KeyValuePair<string, string?>> stringProperties)
        {
            foreach (var property in stringProperties)
            {
                properties[property.Key] = FormatValue(property.Value);
            }

            return;
        }

        if (scope is not null)
        {
            properties["Scope"] = FormatValue(scope);
        }
    }

    private static string? FormatValue(object? value) =>
        value?.ToString()?.ReplaceLineEndings(" ");
}
