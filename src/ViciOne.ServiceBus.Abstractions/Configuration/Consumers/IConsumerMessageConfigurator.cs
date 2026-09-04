using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consumer message configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IConsumerMessageConfigurator<TMessage> :
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for consumer message configurator.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IConsumerMessageConfigurator<TConsumer, TMessage> :
    IPipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>>
    where TConsumer : class
    where TMessage : class
{
    /// <summary>
    /// Add middleware to the consumer pipeline, for the specified message type, which is
    /// invoked after the consumer factory.
    /// </summary>
    /// <param name="configure">The callback to configure the message pipeline</param>
    void Message(Action<IConsumerMessageConfigurator<TMessage>> configure);
}
