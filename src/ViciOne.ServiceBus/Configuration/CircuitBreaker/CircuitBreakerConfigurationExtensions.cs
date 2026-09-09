using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures circuit-breaker middleware on a message pipeline.</summary>
public static class CircuitBreakerConfigurationExtensions
{
    /// <summary>Adds a circuit breaker that stops calls to a failing downstream pipe and admits exactly one recovery probe.</summary>
    /// <typeparam name="T">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to protect.</param>
    /// <param name="configure">An optional callback that customizes failure sampling and recovery.</param>
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
