using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Middleware.Outbox.InMemory;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures volatile outbox buffering and the process-local inbox/outbox store.</summary>
public static class InMemoryOutboxConfigurationExtensions
{
    /// <summary>
    /// Buffers outgoing operations for one message until its consume pipeline completes successfully.
    /// Pending operations are discarded when the pipeline faults.
    /// </summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="configurator">The message consume-pipe configurator to update.</param>
    /// <param name="context">The registration context used to preserve the active consume scope.</param>
    /// <param name="configure">An optional callback that configures outbox delivery.</param>
    public static void UseVolatileOutbox<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, IRegistrationContext context,
        Action<IOutboxConfigurator>? configure = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var specification = new InMemoryOutboxSpecification<T>(context);

        configure?.Invoke(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Buffers outgoing operations for one message until its consume pipeline completes successfully.
    /// Pending operations are discarded when the pipeline faults.
    /// </summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="configurator">The message consume-pipe configurator to update.</param>
    /// <param name="configure">An optional callback that configures outbox delivery.</param>
    public static void UseVolatileOutbox<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, Action<IOutboxConfigurator>? configure = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var specification = new InMemoryOutboxSpecification<T>((ISetScopedConsumeContext?)null);

        configure?.Invoke(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Buffers outgoing operations for every message until its consume pipeline completes successfully.
    /// Pending operations are discarded when the pipeline faults.
    /// </summary>
    /// <param name="configurator">The consume-pipe configurator to update.</param>
    /// <param name="context">The registration context used to preserve active consume scopes.</param>
    /// <param name="configure">An optional callback that configures outbox delivery.</param>
    public static void UseVolatileOutbox(this IConsumePipeConfigurator configurator, IRegistrationContext context,
        Action<IOutboxConfigurator>? configure = default)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var observer = new InMemoryOutboxConfigurationObserver(context, configurator, configure);
    }

    /// <summary>
    /// Buffers outgoing operations for every message until its consume pipeline completes successfully.
    /// Pending operations are discarded when the pipeline faults.
    /// </summary>
    /// <param name="configurator">The consume-pipe configurator to update.</param>
    /// <param name="configure">An optional callback that configures outbox delivery.</param>
    public static void UseVolatileOutbox(this IConsumePipeConfigurator configurator, Action<IOutboxConfigurator>? configure = default)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var observer = new InMemoryOutboxConfigurationObserver((ISetScopedConsumeContext?)null, configurator, configure);
    }

    /// <summary>
    /// Buffers operations produced by a consumer until that consumer completes successfully.
    /// Pending operations are discarded when the consumer faults.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer implementation to decorate.</typeparam>
    /// <param name="configurator">The consumer configurator to update.</param>
    /// <param name="context">The registration context used to preserve the active consume scope.</param>
    /// <param name="configure">An optional callback that configures outbox delivery.</param>
    public static void UseVolatileOutbox<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, IRegistrationContext context,
        Action<IOutboxConfigurator>? configure = default)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var observer = new InMemoryOutboxConsumerConfigurationObserver<TConsumer>(context, configurator, configure);
        configurator.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Buffers operations produced by a consumer until that consumer completes successfully.
    /// Pending operations are discarded when the consumer faults.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer implementation to decorate.</typeparam>
    /// <param name="configurator">The consumer configurator to update.</param>
    /// <param name="configure">An optional callback that configures outbox delivery.</param>
    public static void UseVolatileOutbox<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, Action<IOutboxConfigurator>? configure = default)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var observer = new InMemoryOutboxConsumerConfigurationObserver<TConsumer>((ISetScopedConsumeContext?)null, configurator, configure);
        configurator.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Buffers operations produced by a handler until that handler completes successfully.
    /// Pending operations are discarded when the handler faults.
    /// </summary>
    /// <typeparam name="TMessage">The handled message contract.</typeparam>
    /// <param name="configurator">The handler configurator to update.</param>
    /// <param name="context">The registration context used to preserve the active consume scope.</param>
    /// <param name="configure">An optional callback that configures outbox delivery.</param>
    public static void UseVolatileOutbox<TMessage>(this IHandlerConfigurator<TMessage> configurator, IRegistrationContext context,
        Action<IOutboxConfigurator>? configure = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var observer = new InMemoryOutboxHandlerConfigurationObserver(context, configure);
        configurator.ConnectHandlerConfigurationObserver(observer);
    }

    /// <summary>
    /// Buffers operations produced by a handler until that handler completes successfully.
    /// Pending operations are discarded when the handler faults.
    /// </summary>
    /// <typeparam name="TMessage">The handled message contract.</typeparam>
    /// <param name="configurator">The handler configurator to update.</param>
    /// <param name="configure">An optional callback that configures outbox delivery.</param>
    public static void UseVolatileOutbox<TMessage>(this IHandlerConfigurator<TMessage> configurator, Action<IOutboxConfigurator>? configure = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var observer = new InMemoryOutboxHandlerConfigurationObserver((ISetScopedConsumeContext?)null, configure);
        configurator.ConnectHandlerConfigurationObserver(observer);
    }

    /// <summary>
    /// Adds the process-local inbox/outbox repository used for deterministic tests and single-process scenarios.
    /// </summary>
    /// <param name="collection">The service collection to update.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddInMemoryInboxOutbox(this IServiceCollection collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        collection.TryAddSingleton<InMemoryOutboxMessageRepository>();
        collection.TryAddScoped<IOutboxContextFactory<InMemoryOutboxMessageRepository>, InMemoryOutboxContextFactory>();

        return collection;
    }

    /// <summary>
    /// Adds process-local inbox deduplication and ordered outbox delivery to a receive endpoint.
    /// </summary>
    /// <param name="configurator">The receive-endpoint configurator to update.</param>
    /// <param name="context">The registration context that resolves the process-local repository.</param>
    public static void UseInMemoryInboxOutbox(this IReceiveEndpointConfigurator configurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var observer = new OutboxConsumePipeSpecificationObserver<InMemoryOutboxMessageRepository>(configurator, context);

        configurator.ConnectConsumerConfigurationObserver(observer);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

}
