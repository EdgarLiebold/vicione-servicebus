using System;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures concurrency limits for consumers, handlers, and typed consume pipelines.</summary>
public static class ConsumerConcurrencyLimitConfigurationExtensions
{
    /// <summary>Limits the number of concurrent messages consumed by the consumer, regardless of message type.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="configurator">The consumer to limit.</param>
    /// <param name="concurrencyLimit">The positive concurrency budget shared by all message types handled by the consumer.</param>
    public static void UseConcurrencyLimit<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, int concurrencyLimit)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);

        configurator.ConcurrencyPolicy = ConsumerConcurrencyPolicy.Parallel(concurrencyLimit);
    }

    /// <summary>Limits the consumer's concurrency and exposes serialized runtime adjustments on a management endpoint.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="configurator">The consumer to limit.</param>
    /// <param name="concurrencyLimit">The initial positive concurrency budget shared by all handled message types.</param>
    /// <param name="managementEndpointConfigurator">The endpoint that consumes limit-adjustment commands.</param>
    /// <param name="limiterId">An optional case-insensitive identifier for selective adjustments.</param>
    public static void UseConcurrencyLimit<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, int concurrencyLimit,
        IReceiveEndpointConfigurator managementEndpointConfigurator, string? limiterId = null)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);
        ArgumentNullException.ThrowIfNull(managementEndpointConfigurator);
        if (limiterId != null)
            ArgumentException.ThrowIfNullOrWhiteSpace(limiterId);

        var observer = new ConcurrencyLimitConsumerConfigurationObserver<TConsumer>(configurator, concurrencyLimit, limiterId);
        configurator.ConnectConsumerConfigurationObserver(observer);

        managementEndpointConfigurator.Instance(observer.Limiter, x =>
        {
            x.UseConcurrencyLimit(1);
            x.Message<SetConcurrencyLimit>(m => m.UseMessageRetry(r => r.None()));
        });
    }

    /// <summary>Limits the number of concurrent messages consumed by the handler.</summary>
    /// <typeparam name="TMessage">The handled message type.</typeparam>
    /// <param name="configurator">The handler to limit.</param>
    /// <param name="concurrencyLimit">The positive maximum number of concurrent handler invocations.</param>
    public static void UseConcurrencyLimit<TMessage>(this IHandlerConfigurator<TMessage> configurator, int concurrencyLimit)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);

        var observer = new ConcurrencyLimitHandlerConfigurationObserver(concurrencyLimit);
        configurator.ConnectHandlerConfigurationObserver(observer);
    }

    /// <summary>Limits handler concurrency and exposes serialized runtime adjustments on a management endpoint.</summary>
    /// <typeparam name="TMessage">The handled message type.</typeparam>
    /// <param name="configurator">The handler to limit.</param>
    /// <param name="concurrencyLimit">The initial positive maximum number of concurrent handler invocations.</param>
    /// <param name="managementEndpointConfigurator">The endpoint that consumes limit-adjustment commands.</param>
    /// <param name="limiterId">An optional case-insensitive identifier for selective adjustments.</param>
    public static void UseConcurrencyLimit<TMessage>(this IHandlerConfigurator<TMessage> configurator, int concurrencyLimit,
        IReceiveEndpointConfigurator managementEndpointConfigurator, string? limiterId = null)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);
        ArgumentNullException.ThrowIfNull(managementEndpointConfigurator);
        if (limiterId != null)
            ArgumentException.ThrowIfNullOrWhiteSpace(limiterId);

        var observer = new ConcurrencyLimitHandlerConfigurationObserver(concurrencyLimit, limiterId);
        configurator.ConnectHandlerConfigurationObserver(observer);

        managementEndpointConfigurator.Instance(observer.Limiter, x =>
        {
            x.UseConcurrencyLimit(1);
            x.Message<SetConcurrencyLimit>(m => m.UseMessageRetry(r => r.None()));
        });
    }

    /// <summary>Limits the number of concurrent messages consumed for the specified message type.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="configurator">The typed consume pipeline to limit.</param>
    /// <param name="concurrencyLimit">The positive maximum number of concurrent message deliveries.</param>
    public static void UseConcurrencyLimit<TMessage>(this IPipeConfigurator<ConsumeContext<TMessage>> configurator, int concurrencyLimit)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);

        var limiter = new ConcurrencyLimiter(concurrencyLimit);

        var specification = new ConcurrencyLimitConsumePipeSpecification<TMessage>(limiter);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Limits one business-message type and exposes serialized runtime adjustments on a management endpoint.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="configurator">The typed consume pipeline to limit.</param>
    /// <param name="concurrencyLimit">The initial positive maximum number of concurrent message deliveries.</param>
    /// <param name="managementEndpointConfigurator">The endpoint that consumes limit-adjustment commands.</param>
    /// <param name="limiterId">An optional case-insensitive identifier for selective adjustments.</param>
    public static void UseConcurrencyLimit<TMessage>(this IPipeConfigurator<ConsumeContext<TMessage>> configurator, int concurrencyLimit,
        IReceiveEndpointConfigurator managementEndpointConfigurator, string? limiterId = null)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLimit, 1);
        ArgumentNullException.ThrowIfNull(managementEndpointConfigurator);
        if (typeof(TMessage) == typeof(SetConcurrencyLimit))
        {
            throw new InvalidOperationException(
                $"{nameof(SetConcurrencyLimit)} is the concurrency-control contract and cannot be protected by the limiter it adjusts.");
        }
        if (limiterId != null)
            ArgumentException.ThrowIfNullOrWhiteSpace(limiterId);

        var limiter = new ConcurrencyLimiter(concurrencyLimit, limiterId);

        var specification = new ConcurrencyLimitConsumePipeSpecification<TMessage>(limiter);

        configurator.AddPipeSpecification(specification);

        managementEndpointConfigurator.Instance(limiter, x =>
        {
            x.UseConcurrencyLimit(1);
            x.Message<SetConcurrencyLimit>(m => m.UseMessageRetry(r => r.None()));
        });
    }
}
