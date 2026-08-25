#nullable enable
#nullable enable
namespace ViciOne.ServiceBus.Middleware.CircuitBreaker;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Metadata;
using Monitoring;

internal static class CircuitBreakerTelemetry
{
    private static readonly Meter Meter = new(
        InstrumentationOptions.MeterName,
        HostMetadataCache.Host.ViciOneServiceBusVersion);
    private static readonly ActivitySource ActivitySource = new(
        InstrumentationOptions.MeterName,
        HostMetadataCache.Host.ViciOneServiceBusVersion);
    private static readonly Counter<long> StateTransitions =
        Meter.CreateCounter<long>("vicione.servicebus.circuit_breaker.state_transitions");
    private static readonly Counter<long> Probes =
        Meter.CreateCounter<long>("vicione.servicebus.circuit_breaker.probes");
    private static readonly Counter<long> Rejections =
        Meter.CreateCounter<long>("vicione.servicebus.circuit_breaker.rejections");

    public static void StateTransition(string from, string to)
    {
        TagList tags = default;
        tags.Add("circuit_breaker.state.from", from);
        tags.Add("circuit_breaker.state.to", to);
        StateTransitions.Add(1, tags);

        using Activity? activity = ActivitySource.StartActivity(
            "ViciOne.ServiceBus.CircuitBreaker.StateTransition",
            ActivityKind.Internal);
        activity?.SetTag("circuit_breaker.state.from", from);
        activity?.SetTag("circuit_breaker.state.to", to);
    }

    public static void ProbeAcquired()
    {
        Probes.Add(1);
        using Activity? activity = ActivitySource.StartActivity(
            "ViciOne.ServiceBus.CircuitBreaker.Probe",
            ActivityKind.Internal);
        activity?.SetTag("circuit_breaker.probe.result", "acquired");
    }

    public static void Rejected(bool probeInProgress)
    {
        TagList tags = default;
        tags.Add("circuit_breaker.rejection.reason", probeInProgress ? "probe_in_progress" : "open");
        Rejections.Add(1, tags);

        using Activity? activity = ActivitySource.StartActivity(
            "ViciOne.ServiceBus.CircuitBreaker.Rejected",
            ActivityKind.Internal);
        activity?.SetTag("circuit_breaker.rejection.reason", probeInProgress ? "probe_in_progress" : "open");
    }
}
