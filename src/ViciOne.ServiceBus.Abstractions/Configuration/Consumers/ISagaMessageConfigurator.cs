using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures middleware for a message consumed by a saga.</summary>
/// <typeparam name="TMessage">The message contract carried by the configured consume context.</typeparam>
public interface ISagaMessageConfigurator<TMessage> :
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class
{
}


/// <summary>Configures middleware for a message after its saga instance is loaded.</summary>
/// <typeparam name="TSaga">The saga state carried by the configured saga context.</typeparam>
/// <typeparam name="TMessage">The message contract carried by the configured saga context.</typeparam>
public interface ISagaMessageConfigurator<TSaga, TMessage> :
    IPipeConfigurator<SagaConsumeContext<TSaga, TMessage>>
    where TMessage : class
    where TSaga : class
{
    /// <summary>
    /// Configures the message-specific portion of the saga pipeline.
    /// </summary>
    /// <param name="configure">The callback configuring message-context middleware within the saga pipeline.</param>
    void Message(Action<ISagaMessageConfigurator<TMessage>> configure);
}
