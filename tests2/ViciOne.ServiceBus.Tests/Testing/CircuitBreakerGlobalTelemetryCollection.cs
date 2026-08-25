using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CircuitBreakerGlobalTelemetryCollection
{
    public const string Name = "Circuit breaker global telemetry";
}
