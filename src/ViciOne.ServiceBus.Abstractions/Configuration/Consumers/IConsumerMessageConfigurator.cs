using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures middleware for a consumed message contract.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IConsumerMessageConfigurator<TMessage> :
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class
{
}


/// <summary>Configures middleware for a message contract after a consumer instance is obtained.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IConsumerMessageConfigurator<TConsumer, TMessage> :
    IPipeConfigurator<ConsumerConsumeContext<TConsumer, TMessage>>
    where TConsumer : class
    where TMessage : class
{
    /// <summary>
    /// Configures the message-specific portion of the consumer pipeline.
    /// </summary>
    /// <param name="configure">The callback to configure the message pipeline.</param>
    void Message(Action<IConsumerMessageConfigurator<TMessage>> configure);
}
