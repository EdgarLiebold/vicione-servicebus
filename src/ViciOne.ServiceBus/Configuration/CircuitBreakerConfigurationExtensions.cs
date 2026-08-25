#nullable enable
namespace ViciOne.ServiceBus;

using System;
using Configuration;

public static class CircuitBreakerConfigurationExtensions
{
    /// <summary>
    /// Adds a circuit breaker that stops calls to a failing downstream pipe and admits exactly one recovery probe.
    /// </summary>
    public static void UseCircuitBreaker<T>(
        this IPipeConfigurator<T> configurator,
        Action<CircuitBreakerOptions>? configure = null)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var options = new CircuitBreakerOptions();
        configure?.Invoke(options);

        configurator.AddPipeSpecification(new CircuitBreakerPipeSpecification<T>(options.CreateSettings()));
    }
}
