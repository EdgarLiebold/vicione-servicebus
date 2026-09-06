using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures saga message.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISagaMessageConfigurator<TMessage> :
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class
{
}


/// <summary>Configures saga message.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISagaMessageConfigurator<TSaga, TMessage> :
    IPipeConfigurator<SagaConsumeContext<TSaga, TMessage>>
    where TMessage : class
    where TSaga : class
{
    /// <summary>
    /// Add middleware to the saga pipeline, for the specified message type, which is
    /// invoked after the saga repository.
    /// </summary>
    /// <param name="configure">The callback to configure the message pipeline.</param>
    void Message(Action<ISagaMessageConfigurator<TMessage>> configure);
}
