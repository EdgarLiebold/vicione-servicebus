using System;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Connects Event Hubs receive endpoints to a running rider.</summary>
public interface IEventHubEndpointConnector
{
    /// <summary>Connects and starts a receive endpoint for an Event Hub consumer group.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="consumerGroup">The consumer group used to coordinate partition ownership.</param>
    /// <param name="configure">Configures the connected receive endpoint.</param>
    /// <returns>A handle for observing readiness and stopping the connected endpoint.</returns>
    IHostReceiveEndpointHandle ConnectEventHubEndpoint(string eventHubName, string consumerGroup,
        Action<IRiderRegistrationContext, IEventHubReceiveEndpointConfigurator> configure);
}
