using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumer;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Provides extension methods for batch consumer.</summary>
public static class BatchConsumerExtensions
{
    /// <summary>
    /// Configure a Batch&lt;<typeparamref name="TMessage" />&gt; consumer, which allows messages to be collected into an array and consumed
    /// at once. This feature is experimental, but often requested. Be sure to configure the transport with sufficient concurrent message
    /// capacity (prefetch, etc.) so that a batch can actually complete without always reaching the time limit.
    /// </summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void Batch<TMessage>(this IReceiveEndpointConfigurator configurator, Action<IBatchConfigurator<TMessage>> configure)
        where TMessage : class
    {
        LogContext.Debug?.Log("Configuring batch: {MessageType}", TypeCache<TMessage>.ShortName);

        var batchConfigurator = new BatchConfigurator<TMessage>(configurator);

        configure?.Invoke(batchConfigurator);
    }

    /// <summary>Connect a consumer with a consumer factory method.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="consumerFactoryMethod">The consumer factory method.</param>
    public static void Consumer<TConsumer, TMessage>(this IBatchConfigurator<TMessage> configurator, Func<TConsumer> consumerFactoryMethod)
        where TConsumer : class, IConsumer<Batch<TMessage>>
        where TMessage : class
    {
        LogContext.Debug?.Log("Subscribing Batch Consumer: {ConsumerType} (using delegate consumer factory)", TypeCache<TConsumer>.ShortName);

        var delegateConsumerFactory = new DelegateConsumerFactory<TConsumer>(consumerFactoryMethod);

        configurator.Consumer(delegateConsumerFactory);
    }

    /// <summary>Connect a consumer with a consumer factory method.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    public static void Consumer<TConsumer, TMessage>(this IBatchConfigurator<TMessage> configurator, IConsumerFactory<TConsumer> consumerFactory)
        where TConsumer : class, IConsumer<Batch<TMessage>>
        where TMessage : class
    {
        LogContext.Debug?.Log("Subscribing Batch Consumer: {ConsumerType} (using supplied consumer factory)", TypeCache<TConsumer>.ShortName);

        configurator.Consumer(consumerFactory);
    }
}
