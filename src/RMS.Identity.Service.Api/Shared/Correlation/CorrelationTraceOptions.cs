namespace RMS.Identity.Service.Api.Shared.Correlation;

public sealed class CorrelationTraceOptions
{
    public const string SectionName = "CorrelationTrace";

    public string RequestHeaderName { get; set; } = "X-Correlation-Trace-ID";

    public string ResponseHeaderName { get; set; } = "X-Correlation-Trace-ID";

    public int MaxLength { get; set; } = 128;
}
