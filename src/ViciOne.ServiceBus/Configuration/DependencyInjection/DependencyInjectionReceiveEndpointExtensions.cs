using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides dependency-injection receive-endpoint extensions for consumers.</summary>
public static class DependencyInjectionReceiveEndpointExtensions
{
    /// <summary>Registers a consumer given the lifetime scope specified.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <param name="configurator">The service bus configurator.</param>
    /// <param name="context">The LifetimeScope of the provider.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void Consumer<T>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        IConsumeScopeProvider scopeProvider = new ConsumeScopeProvider(context);

        var consumerFactory = new ScopeConsumerFactory<T>(scopeProvider);

        configurator.Consumer(consumerFactory, configure);
    }


    /// <summary>Connect a consumer with a consumer factory method.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void Consumer<TConsumer, TMessage>(this IBatchConfigurator<TMessage> configurator, IRegistrationContext context,
        Action<IConsumerMessageConfigurator<TConsumer, Batch<TMessage>>>? configure = null)
        where TConsumer : class, IConsumer<Batch<TMessage>>
        where TMessage : class
    {
        IConsumeScopeProvider scopeProvider = new ConsumeScopeProvider(context);

        IConsumerFactory<TConsumer> consumerFactory = new ScopeConsumerFactory<TConsumer>(scopeProvider);

        configurator.Consumer(consumerFactory, configure);
    }


    /// <summary>Connect a consumer to the bus/mediator.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <param name="connector">The connector.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="pipeSpecifications">The pipe specifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectConsumer<TConsumer>(this IConsumePipeConnector connector, IRegistrationContext context,
        params IPipeSpecification<ConsumerConsumeContext<TConsumer>>[] pipeSpecifications)
        where TConsumer : class, IConsumer
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        IConsumeScopeProvider scopeProvider = new ConsumeScopeProvider(context);

        IConsumerFactory<TConsumer> consumerFactory = new ScopeConsumerFactory<TConsumer>(scopeProvider);

        return connector.ConnectConsumer(consumerFactory, pipeSpecifications);
    }
}
