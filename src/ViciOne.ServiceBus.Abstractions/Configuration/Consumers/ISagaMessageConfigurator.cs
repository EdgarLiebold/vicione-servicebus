using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga message configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISagaMessageConfigurator<TMessage> :
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for saga message configurator.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISagaMessageConfigurator<TSaga, TMessage> :
    IPipeConfigurator<SagaConsumeContext<TSaga, TMessage>>
    where TMessage : class
    where TSaga : class
{
    /// <summary>
    /// Add middleware to the saga pipeline, for the specified message type, which is
    /// invoked after the saga repository.
    /// </summary>
    /// <param name="configure">The callback to configure the message pipeline</param>
    void Message(Action<ISagaMessageConfigurator<TMessage>> configure);
}
