using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by consumer message connector.</summary>
public interface IConsumerMessageConnector
{
    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }
}


/// <summary>Defines the operations required by consumer message connector.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface IConsumerMessageConnector<TConsumer> :
    IConsumerMessageConnector
    where TConsumer : class
{
    /// <summary>Creates consumer message specification.</summary>
    /// <returns>The created consumer message specification.</returns>
    IConsumerMessageSpecification<TConsumer> CreateConsumerMessageSpecification();

    /// <summary>Connects consumer.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumer(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification);
}
