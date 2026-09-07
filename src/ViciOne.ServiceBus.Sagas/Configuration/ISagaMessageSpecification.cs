using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for saga message.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaMessageSpecification<TSaga> :
    IPipeConfigurator<SagaConsumeContext<TSaga>>,
    ISagaConfigurationObserverConnector,
    ISpecification
    where TSaga : class, ISaga
{
    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }

    /// <summary>Gets message specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message specification.</returns>
    ISagaMessageSpecification<TSaga, T> GetMessageSpecification<T>()
        where T : class;
}


/// <summary>Describes requirements for saga message.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISagaMessageSpecification<TSaga, TMessage> :
    ISagaMessageSpecification<TSaga>,
    ISagaMessageConfigurator<TSaga, TMessage>,
    ISagaMessageConfigurator<TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Build the consumer pipe, using the consume filter specified.</summary>
    /// <param name="consumeFilter">The consume filter.</param>
    /// <returns>The configured consumer pipe.</returns>
    IPipe<SagaConsumeContext<TSaga, TMessage>> BuildConsumerPipe(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter);

    /// <summary>
    /// Configure the message pipe as it is built. Any previously configured filters will precede
    /// the configuration applied by the <paramref name="configure" /> callback.
    /// </summary>
    /// <param name="configure">Configure the message pipe.</param>
    /// <returns>The configured message pipe.</returns>
    IPipe<ConsumeContext<TMessage>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TMessage>>> configure);
}
