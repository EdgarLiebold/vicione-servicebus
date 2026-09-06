using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Builds Event Hubs producer transports, receive specifications, and rider instances from shared host configuration.</summary>
public interface IEventHubHostConfiguration :
    ISpecification
{
    /// <summary>Gets the supervisor that owns shared Event Hubs connection state.</summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>Creates a send transport context for a named Event Hub.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="busInstance">The bus instance supplying host and topology services.</param>
    /// <returns>The configured Event Hubs send transport context.</returns>
    EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance);

    /// <summary>Creates a receive-endpoint specification for an Event Hub consumer group.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="consumerGroup">The consumer group used to coordinate partition ownership.</param>
    /// <param name="configure">Configures the receive endpoint when it is built.</param>
    /// <returns>The Event Hubs receive-endpoint specification.</returns>
    IEventHubReceiveEndpointSpecification CreateSpecification(string eventHubName, string consumerGroup,
        Action<IEventHubReceiveEndpointConfigurator> configure);

    /// <summary>Builds the Event Hubs rider for a bus instance.</summary>
    /// <param name="context">The rider registration context.</param>
    /// <param name="busInstance">The bus instance that will own the rider.</param>
    /// <returns>The configured Event Hubs rider.</returns>
    IEventHubRider Build(IRiderRegistrationContext context, IBusInstance busInstance);
}
