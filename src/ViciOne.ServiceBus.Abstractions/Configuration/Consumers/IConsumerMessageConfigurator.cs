using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures consumer message.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IConsumerMessageConfigurator<TMessage> :
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class
{
}


/// <summary>Configures consumer message.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IConsumerMessageConfigurator<TConsumer, TMessage> :
    IPipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>>
    where TConsumer : class
    where TMessage : class
{
    /// <summary>
    /// Add middleware to the consumer pipeline, for the specified message type, which is
    /// invoked after the consumer factory.
    /// </summary>
    /// <param name="configure">The callback to configure the message pipeline.</param>
    void Message(Action<IConsumerMessageConfigurator<TMessage>> configure);
}
