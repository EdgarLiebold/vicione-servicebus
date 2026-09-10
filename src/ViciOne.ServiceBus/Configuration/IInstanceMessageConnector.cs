using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Identifies the message contract handled by an instance-message connector.</summary>
public interface IInstanceMessageConnector
{
    /// <summary>Gets the message contract accepted by this connector.</summary>
    Type MessageType { get; }
}


/// <summary>Builds and connects the message pipeline for an existing consumer instance.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
public interface IInstanceMessageConnector<TConsumer> :
    IInstanceMessageConnector
    where TConsumer : class
{
    /// <summary>Creates an independent specification for this consumer-message pipeline.</summary>
    /// <returns>A new consumer-message specification.</returns>
    IConsumerMessageSpecification<TConsumer> CreateConsumerMessageSpecification();

    /// <summary>Connects this message contract for an existing consumer instance.</summary>
    /// <param name="pipeConnector">The consume pipe that will dispatch matching messages.</param>
    /// <param name="instance">The consumer instance to invoke.</param>
    /// <param name="specification">The consumer and message pipeline configuration to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectInstance(IConsumePipeConnector pipeConnector, TConsumer instance, IConsumerSpecification<TConsumer> specification);
}
