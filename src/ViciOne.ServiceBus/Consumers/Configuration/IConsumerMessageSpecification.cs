using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consumer message specification.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public interface IConsumerMessageSpecification<TConsumer> :
    IPipeConfigurator<ConsumerConsumeContext<TConsumer>>,
    IConsumerConfigurationObserverConnector,
    ISpecification
    where TConsumer : class
{
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// Attempts to get message specification.
    /// </summary>
    /// <typeparam name="TC">The tc type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageSpecification<TC, T>([NotNullWhen(true)] out IConsumerMessageSpecification<TC, T>? specification)
        where T : class
        where TC : class;
}


/// <summary>
/// Defines the contract for consumer message specification.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IConsumerMessageSpecification<TConsumer, TMessage> :
    IConsumerMessageSpecification<TConsumer>,
    IConsumerMessageConfigurator<TConsumer, TMessage>,
    IConsumerMessageConfigurator<TMessage>
    where TConsumer : class
    where TMessage : class
{
    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="consumeFilter">The consume filter value.</param>
    /// <returns>The result of the operation.</returns>
    IPipe<ConsumerConsumeContext<TConsumer, TMessage>> Build(IFilter<ConsumerConsumeContext<TConsumer, TMessage>> consumeFilter);

    /// <summary>
    /// Configure the message pipe as it is built. Any previously configured filters will precede
    /// the configuration applied by the <paramref name="configure" /> callback.
    /// </summary>
    /// <param name="configure">Configure the message pipe</param>
    /// <returns></returns>
    IPipe<ConsumeContext<TMessage>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TMessage>>> configure);
}
