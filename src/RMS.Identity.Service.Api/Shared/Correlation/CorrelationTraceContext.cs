namespace RMS.Identity.Service.Api.Shared.Correlation;

public static class CorrelationTraceContext
{
    public const string ItemKey = "CorrelationTraceId";

    public static string? GetCorrelationTraceId(HttpContext context) =>
        context.Items.TryGetValue(ItemKey, out var value)
            ? value as string
            : null;
}
