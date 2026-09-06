using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for circuit breaker configuration.</summary>
public static class CircuitBreakerConfigurationExtensions
{
    /// <summary>Adds a circuit breaker that stops calls to a failing downstream pipe and admits exactly one recovery probe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
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
