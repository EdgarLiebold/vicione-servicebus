using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes a discovered consumer message contract and creates its typed connectors.</summary>
public interface IMessageInterfaceType
{
    /// <summary>Gets the convention-discovered message contract.</summary>
    Type MessageType { get; }

    /// <summary>Gets the connector used with a factory-created consumer.</summary>
    /// <typeparam name="TConsumer">The consumer type requested by the caller.</typeparam>
    /// <returns>The connector typed for the requested consumer.</returns>
    IConsumerMessageConnector<TConsumer> GetConsumerConnector<TConsumer>()
        where TConsumer : class;

    /// <summary>Gets the connector used with an existing consumer instance.</summary>
    /// <typeparam name="TConsumer">The consumer type requested by the caller.</typeparam>
    /// <returns>The connector typed for the requested consumer.</returns>
    IInstanceMessageConnector<TConsumer> GetInstanceConnector<TConsumer>()
        where TConsumer : class;
}
