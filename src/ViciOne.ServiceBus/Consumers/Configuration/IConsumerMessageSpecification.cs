using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for consumer message.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface IConsumerMessageSpecification<TConsumer> :
    IPipeConfigurator<ConsumerConsumeContext<TConsumer>>,
    IConsumerConfigurationObserverConnector,
    ISpecification
    where TConsumer : class
{
    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }

    /// <summary>Attempts to get message specification.</summary>
    /// <typeparam name="TC">The c type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">Receives the specification produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSpecification<TC, T>([NotNullWhen(true)] out IConsumerMessageSpecification<TC, T>? specification)
        where T : class
        where TC : class;
}


/// <summary>Describes requirements for consumer message.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IConsumerMessageSpecification<TConsumer, TMessage> :
    IConsumerMessageSpecification<TConsumer>,
    IConsumerMessageConfigurator<TConsumer, TMessage>,
    IConsumerMessageConfigurator<TMessage>
    where TConsumer : class
    where TMessage : class
{
    /// <summary>Builds the configured component.</summary>
    /// <param name="consumeFilter">The consume filter.</param>
    /// <returns>The configured component.</returns>
    IPipe<ConsumerConsumeContext<TConsumer, TMessage>> Build(IFilter<ConsumerConsumeContext<TConsumer, TMessage>> consumeFilter);

    /// <summary>
    /// Configure the message pipe as it is built. Any previously configured filters will precede
    /// the configuration applied by the <paramref name="configure" /> callback.
    /// </summary>
    /// <param name="configure">Configure the message pipe.</param>
    /// <returns>The configured message pipe.</returns>
    IPipe<ConsumeContext<TMessage>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TMessage>>> configure);
}
