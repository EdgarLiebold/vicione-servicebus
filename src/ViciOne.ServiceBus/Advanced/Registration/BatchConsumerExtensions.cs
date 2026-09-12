using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumers;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Registers consumers that receive bounded batches of messages.</summary>
public static class BatchConsumerExtensions
{
    /// <summary>
    /// Adds batch collection for <typeparamref name="TMessage" /> to a receive endpoint. The endpoint must admit enough
    /// concurrent messages for the configured batch size to be reached before its time limit.
    /// </summary>
    /// <typeparam name="TMessage">The individual message contract collected into each batch.</typeparam>
    /// <param name="configurator">The receive endpoint that will collect the messages.</param>
    /// <param name="configure">The callback that defines collection limits and the batch consumer.</param>
    public static void Batch<TMessage>(this IReceiveEndpointConfigurator configurator, Action<IBatchConfigurator<TMessage>> configure)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        LogContext.Debug?.Log("Configuring batch: {MessageType}", TypeCache<TMessage>.ShortName);

        var batchConfigurator = new BatchConfigurator<TMessage>(configurator);

        configure(batchConfigurator);
    }

    /// <summary>Registers a batch consumer created by the supplied factory delegate.</summary>
    /// <typeparam name="TConsumer">The consumer implementation that receives completed batches.</typeparam>
    /// <typeparam name="TMessage">The individual message contract collected into each batch.</typeparam>
    /// <param name="configurator">The batch configuration that owns the registration.</param>
    /// <param name="consumerFactoryMethod">The delegate that creates a batch consumer for each delivery.</param>
    public static void Consumer<TConsumer, TMessage>(this IBatchConfigurator<TMessage> configurator, Func<TConsumer> consumerFactoryMethod)
        where TConsumer : class, IConsumer<IMessageBatch<TMessage>>
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(consumerFactoryMethod);

        LogContext.Debug?.Log("Subscribing Batch Consumer: {ConsumerType} (using delegate consumer factory)", TypeCache<TConsumer>.ShortName);

        var delegateConsumerFactory = new DelegateConsumerFactory<TConsumer>(consumerFactoryMethod);

        configurator.Consumer(delegateConsumerFactory);
    }

    /// <summary>Registers a batch consumer supplied by an <see cref="IConsumerFactory{TConsumer}" />.</summary>
    /// <typeparam name="TConsumer">The consumer implementation that receives completed batches.</typeparam>
    /// <typeparam name="TMessage">The individual message contract collected into each batch.</typeparam>
    /// <param name="configurator">The batch configuration that owns the registration.</param>
    /// <param name="consumerFactory">The factory that supplies a batch consumer for each delivery.</param>
    public static void Consumer<TConsumer, TMessage>(this IBatchConfigurator<TMessage> configurator, IConsumerFactory<TConsumer> consumerFactory)
        where TConsumer : class, IConsumer<IMessageBatch<TMessage>>
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(consumerFactory);

        LogContext.Debug?.Log("Subscribing Batch Consumer: {ConsumerType} (using supplied consumer factory)", TypeCache<TConsumer>.ShortName);

        configurator.Consumer(consumerFactory);
    }
}
