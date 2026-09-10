using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Identifies the message contract handled by a consumer-message connector.</summary>
public interface IConsumerMessageConnector
{
    /// <summary>Gets the message contract accepted by this connector.</summary>
    Type MessageType { get; }
}


/// <summary>Builds and connects the message pipeline for one consumer type.</summary>
/// <typeparam name="TConsumer">The consumer implementation invoked by the message pipeline.</typeparam>
public interface IConsumerMessageConnector<TConsumer> :
    IConsumerMessageConnector
    where TConsumer : class
{
    /// <summary>Creates an independent message-pipeline specification.</summary>
    /// <returns>The new consumer message specification.</returns>
    IConsumerMessageSpecification<TConsumer> CreateConsumerMessageSpecification();

    /// <summary>Connects the configured consumer message pipeline.</summary>
    /// <param name="consumePipe">The consume pipe that will dispatch matching messages.</param>
    /// <param name="consumerFactory">The factory that supplies a consumer for each delivery.</param>
    /// <param name="specification">The consumer and message pipeline configuration to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumer(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification);
}
