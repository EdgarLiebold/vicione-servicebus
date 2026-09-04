using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consumer message connector.
/// </summary>
public interface IConsumerMessageConnector
{
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    Type MessageType { get; }
}


/// <summary>
/// Defines the contract for consumer message connector.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public interface IConsumerMessageConnector<TConsumer> :
    IConsumerMessageConnector
    where TConsumer : class
{
    /// <summary>
    /// Creates consumer message specification.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IConsumerMessageSpecification<TConsumer> CreateConsumerMessageSpecification();

    /// <summary>
    /// Connects consumer.
    /// </summary>
    /// <param name="consumePipe">The consume pipe value.</param>
    /// <param name="consumerFactory">The consumer factory value.</param>
    /// <param name="specification">The specification value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConsumer(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification);
}
