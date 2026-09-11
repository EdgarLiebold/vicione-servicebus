namespace ViciOne.ServiceBus.Logging.Diagnostics;

/// <summary>Defines private transport headers used to propagate distributed-tracing context.</summary>
internal static class DiagnosticPropagationHeaders
{
    internal const string LegacyAzureDiagnosticId = "Diagnostic-Id";
    internal const string ActivityId = MessageHeaders.Prefix + "Activity-Id";
    internal const string Baggage = MessageHeaders.Prefix + "Activity-Correlation-Context";
    internal const string TraceState = MessageHeaders.Prefix + "Activity-Trace-State";
    internal const string ParentMode = MessageHeaders.Prefix + "Activity-Propagation";
}
