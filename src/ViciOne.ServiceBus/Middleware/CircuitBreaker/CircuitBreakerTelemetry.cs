using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Monitoring;

namespace ViciOne.ServiceBus.Middleware.CircuitBreaker;

internal static class CircuitBreakerTelemetry
{
    private static readonly Lazy<Instrumentation> _instruments = new(
        static () => new Instrumentation(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static void StateTransition(string from, string to)
    {
        try
        {
            TagList tags = default;
            tags.Add("circuit_breaker.state.from", from);
            tags.Add("circuit_breaker.state.to", to);
            _instruments.Value.StateTransitions.Add(1, tags);

            using Activity? activity = _instruments.Value.ActivitySource.StartActivity(
                "ViciOne.ServiceBus.CircuitBreaker.StateTransition",
                ActivityKind.Internal);
            activity?.SetTag("circuit_breaker.state.from", from);
            activity?.SetTag("circuit_breaker.state.to", to);
        }
        catch (Exception)
        {
            // In-process telemetry observers never own message-delivery or circuit-breaker semantics.
        }
    }

    public static void ProbeAcquired()
    {
        try
        {
            _instruments.Value.Probes.Add(1);
            using Activity? activity = _instruments.Value.ActivitySource.StartActivity(
                "ViciOne.ServiceBus.CircuitBreaker.Probe",
                ActivityKind.Internal);
            activity?.SetTag("circuit_breaker.probe.result", "acquired");
        }
        catch (Exception)
        {
            // In-process telemetry observers never own message-delivery or circuit-breaker semantics.
        }
    }

    public static void Rejected(bool probeInProgress)
    {
        try
        {
            string reason = probeInProgress ? "probe_in_progress" : "open";
            TagList tags = default;
            tags.Add("circuit_breaker.rejection.reason", reason);
            _instruments.Value.Rejections.Add(1, tags);

            using Activity? activity = _instruments.Value.ActivitySource.StartActivity(
                "ViciOne.ServiceBus.CircuitBreaker.Rejected",
                ActivityKind.Internal);
            activity?.SetTag("circuit_breaker.rejection.reason", reason);
        }
        catch (Exception)
        {
            // In-process telemetry observers never own message-delivery or circuit-breaker semantics.
        }
    }

    private sealed class Instrumentation
    {
        private readonly Meter _meter;

        public Instrumentation()
        {
            string? version = HostMetadataCache.Host.ViciOneServiceBusVersion;
            _meter = new Meter(ServiceBusTelemetry.MeterName, version);
            ActivitySource = new ActivitySource(ServiceBusTelemetry.ActivitySourceName, version);
            StateTransitions = _meter.CreateCounter<long>("vicione.servicebus.circuit_breaker.state_transitions");
            Probes = _meter.CreateCounter<long>("vicione.servicebus.circuit_breaker.probes");
            Rejections = _meter.CreateCounter<long>("vicione.servicebus.circuit_breaker.rejections");
        }

        public ActivitySource ActivitySource { get; }
        public Counter<long> StateTransitions { get; }
        public Counter<long> Probes { get; }
        public Counter<long> Rejections { get; }
    }
}
