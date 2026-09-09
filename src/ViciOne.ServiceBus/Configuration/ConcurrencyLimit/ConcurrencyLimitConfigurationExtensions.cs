using System;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures concurrency admission on general and consume pipelines.</summary>
public static class ConcurrencyLimitConfigurationExtensions
{
    /// <summary>
    /// Limits concurrent execution in every downstream stage of the pipeline.
    /// </summary>
    /// <typeparam name="T">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to limit.</param>
    /// <param name="concurrencyLimit">The positive maximum number of concurrent downstream operations.</param>
    /// <param name="router">An optional control router for runtime limit adjustments.</param>
    public static void UseConcurrencyLimit<T>(this IPipeConfigurator<T> configurator, int concurrencyLimit, IPipeRouter? router = null)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);

        var specification = new ConcurrencyLimitPipeSpecification<T>(concurrencyLimit, router);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Limits the number of concurrent messages consumed on the receive endpoint, regardless of message type.</summary>
    /// <param name="configurator">The consume pipeline to limit.</param>
    /// <param name="concurrencyLimit">The positive concurrency budget shared by all message types.</param>
    public static void UseConcurrencyLimit(this IConsumePipeConfigurator configurator, int concurrencyLimit)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);

        _ = new ConcurrencyLimitConfigurationObserver(configurator, concurrencyLimit);
    }

    /// <summary>Limits endpoint-wide consumption and exposes serialized runtime adjustments on a management endpoint.</summary>
    /// <param name="configurator">The consume pipeline to limit.</param>
    /// <param name="concurrencyLimit">The initial positive concurrency budget shared by all message types.</param>
    /// <param name="managementEndpointConfigurator">The endpoint that consumes limit-adjustment commands.</param>
    /// <param name="limiterId">An optional case-insensitive identifier for selective adjustments.</param>
    public static void UseConcurrencyLimit(this IConsumePipeConfigurator configurator, int concurrencyLimit,
        IReceiveEndpointConfigurator managementEndpointConfigurator, string? limiterId = default)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);
        ArgumentNullException.ThrowIfNull(managementEndpointConfigurator);
        if (limiterId != null)
            ArgumentException.ThrowIfNullOrWhiteSpace(limiterId);

        var observer = new ConcurrencyLimitConfigurationObserver(
            configurator,
            concurrencyLimit,
            limiterId,
            excludeManagementCommands: true);

        managementEndpointConfigurator.Instance(observer.Limiter, x =>
        {
            x.UseConcurrencyLimit(1);
            x.Message<SetConcurrencyLimit>(m => m.UseMessageRetry(r => r.None()));
        });
    }
}
