using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures and validates a saga message pipeline with saga-configuration observation.</summary>
/// <typeparam name="TSaga">The saga state carried by the configured pipeline.</typeparam>
public interface ISagaMessageSpecification<TSaga> :
    IPipeConfigurator<SagaConsumeContext<TSaga>>,
    ISagaConfigurationObserverConnector,
    ISpecification
    where TSaga : class, ISaga
{
    /// <summary>Gets the message contract associated with this specification.</summary>
    Type MessageType { get; }

    /// <summary>Obtains the specification for the requested message contract.</summary>
    /// <typeparam name="T">The message contract required by the caller.</typeparam>
    /// <returns>The matching message-specific saga specification.</returns>
    ISagaMessageSpecification<TSaga, T> GetMessageSpecification<T>()
        where T : class;
}


/// <summary>Builds saga-context and message-context pipelines for one saga message contract.</summary>
/// <typeparam name="TSaga">The saga state carried by the saga-context pipeline.</typeparam>
/// <typeparam name="TMessage">The message contract carried by both configured pipelines.</typeparam>
public interface ISagaMessageSpecification<TSaga, TMessage> :
    ISagaMessageSpecification<TSaga>,
    ISagaMessageConfigurator<TSaga, TMessage>,
    ISagaMessageConfigurator<TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Builds the saga-context pipeline with the supplied consume filter.</summary>
    /// <param name="consumeFilter">The filter consuming the message in its selected saga context.</param>
    /// <returns>The configured saga-context consumer pipeline.</returns>
    IPipe<SagaConsumeContext<TSaga, TMessage>> BuildConsumerPipe(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter);

    /// <summary>
    /// Configure the message pipe as it is built. Any previously configured filters will precede
    /// the configuration applied by the <paramref name="configure" /> callback.
    /// </summary>
    /// <param name="configure">The callback appending message-context pipeline configuration.</param>
    /// <returns>The configured message-context pipeline.</returns>
    IPipe<ConsumeContext<TMessage>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TMessage>>> configure);
}
