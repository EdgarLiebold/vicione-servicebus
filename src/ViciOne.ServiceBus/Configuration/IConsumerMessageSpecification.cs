using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides message-erased access to one consumer message-pipeline specification.</summary>
/// <typeparam name="TConsumer">The consumer implementation configured by the specification.</typeparam>
public interface IConsumerMessageSpecification<TConsumer> :
    IPipeConfigurator<ConsumerConsumeContext<TConsumer>>,
    IConsumerConfigurationObserverConnector,
    ISpecification
    where TConsumer : class
{
    /// <summary>Gets the message contract configured by this specification.</summary>
    Type MessageType { get; }

    /// <summary>Attempts to project this specification to a requested consumer and message pair.</summary>
    /// <typeparam name="TRequestedConsumer">The requested consumer type.</typeparam>
    /// <typeparam name="TRequestedMessage">The requested message contract.</typeparam>
    /// <param name="specification">Receives this specification when both requested types match.</param>
    /// <returns><see langword="true" /> when this instance implements the requested closed specification type.</returns>
    bool TryGetMessageSpecification<TRequestedConsumer, TRequestedMessage>(
        [NotNullWhen(true)] out IConsumerMessageSpecification<TRequestedConsumer, TRequestedMessage>? specification)
        where TRequestedMessage : class
        where TRequestedConsumer : class;
}


/// <summary>Configures and builds the message pipelines for one consumer and message pair.</summary>
/// <typeparam name="TConsumer">The consumer implementation configured by the specification.</typeparam>
/// <typeparam name="TMessage">The message contract delivered to that consumer.</typeparam>
public interface IConsumerMessageSpecification<TConsumer, TMessage> :
    IConsumerMessageSpecification<TConsumer>,
    IConsumerMessageConfigurator<TConsumer, TMessage>,
    IConsumerMessageConfigurator<TMessage>
    where TConsumer : class
    where TMessage : class
{
    /// <summary>Appends the consumer invocation filter and builds the closed consumer-message pipe.</summary>
    /// <param name="consumeFilter">The terminal filter that invokes the consumer contract.</param>
    /// <returns>The configured consumer-message pipe.</returns>
    IPipe<ConsumerConsumeContext<TConsumer, TMessage>> Build(IFilter<ConsumerConsumeContext<TConsumer, TMessage>> consumeFilter);

    /// <summary>
    /// Applies final configuration and builds the inbound message pipe. Previously configured filters precede
    /// the stages appended by the <paramref name="configure" /> callback.
    /// </summary>
    /// <param name="configure">The callback that appends the consumer dispatch stage.</param>
    /// <returns>The configured message pipe.</returns>
    IPipe<ConsumeContext<TMessage>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TMessage>>> configure);
}
