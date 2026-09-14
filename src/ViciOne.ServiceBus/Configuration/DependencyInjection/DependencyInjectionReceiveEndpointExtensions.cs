using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers dependency-injection-owned consumers with receive and consume pipelines.</summary>
public static class DependencyInjectionReceiveEndpointExtensions
{
    /// <summary>Registers a scoped consumer with a receive endpoint.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="configurator">The receive endpoint that owns the registration.</param>
    /// <param name="context">The registration context that creates consumer scopes.</param>
    /// <param name="configure">An optional callback that configures the consumer pipeline.</param>
    public static void Consumer<T>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        IConsumeScopeProvider scopeProvider = new ConsumeScopeProvider(context);

        var consumerFactory = new ScopeConsumerFactory<T>(scopeProvider);

        configurator.Consumer(consumerFactory, configure);
    }
    /// <summary>Registers a scoped consumer for completed batches.</summary>
    /// <typeparam name="TConsumer">The consumer implementation that receives each batch.</typeparam>
    /// <typeparam name="TMessage">The individual message contract collected into the batch.</typeparam>
    /// <param name="configurator">The batch configuration that owns the registration.</param>
    /// <param name="context">The registration context that creates consumer scopes.</param>
    /// <param name="configure">An optional callback that configures the batch consumer pipeline.</param>
    public static void Consumer<TConsumer, TMessage>(this IBatchConfigurator<TMessage> configurator, IRegistrationContext context,
        Action<IConsumerMessageConfigurator<TConsumer, IMessageBatch<TMessage>>>? configure = null)
        where TConsumer : class, IConsumer<IMessageBatch<TMessage>>
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        IConsumeScopeProvider scopeProvider = new ConsumeScopeProvider(context);

        IConsumerFactory<TConsumer> consumerFactory = new ScopeConsumerFactory<TConsumer>(scopeProvider);

        configurator.Consumer(consumerFactory, configure);
    }
    /// <summary>Connects a scoped consumer to an existing consume pipeline.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="connector">The consume pipeline to extend.</param>
    /// <param name="context">The registration context that creates consumer scopes.</param>
    /// <param name="pipeSpecifications">The middleware specifications applied to the consumer.</param>
    /// <returns>A handle that disconnects the consumer.</returns>
    public static ConnectHandle ConnectConsumer<TConsumer>(this IConsumePipeConnector connector, IRegistrationContext context,
        params IPipeSpecification<ConsumerConsumeContext<TConsumer>>[] pipeSpecifications)
        where TConsumer : class, IConsumer
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pipeSpecifications);
        if (Array.Exists(pipeSpecifications, static specification => specification is null))
            throw new ArgumentException("Pipe specifications must not contain null elements.", nameof(pipeSpecifications));

        IConsumeScopeProvider scopeProvider = new ConsumeScopeProvider(context);

        IConsumerFactory<TConsumer> consumerFactory = new ScopeConsumerFactory<TConsumer>(scopeProvider);

        return connector.ConnectConsumer(consumerFactory, pipeSpecifications);
    }
}
