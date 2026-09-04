using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga message specification.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ISagaMessageSpecification<TSaga> :
    IPipeConfigurator<SagaConsumeContext<TSaga>>,
    ISagaConfigurationObserverConnector,
    ISpecification
    where TSaga : class, ISaga
{
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// Gets message specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    ISagaMessageSpecification<TSaga, T> GetMessageSpecification<T>()
        where T : class;
}


/// <summary>
/// Defines the contract for saga message specification.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISagaMessageSpecification<TSaga, TMessage> :
    ISagaMessageSpecification<TSaga>,
    ISagaMessageConfigurator<TSaga, TMessage>,
    ISagaMessageConfigurator<TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>
    /// Build the consumer pipe, using the consume filter specified.
    /// </summary>
    /// <param name="consumeFilter"></param>
    /// <returns></returns>
    IPipe<SagaConsumeContext<TSaga, TMessage>> BuildConsumerPipe(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter);

    /// <summary>
    /// Configure the message pipe as it is built. Any previously configured filters will precede
    /// the configuration applied by the <paramref name="configure" /> callback.
    /// </summary>
    /// <param name="configure">Configure the message pipe</param>
    /// <returns></returns>
    IPipe<ConsumeContext<TMessage>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TMessage>>> configure);
}
