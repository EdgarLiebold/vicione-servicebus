using System;
using Azure.Messaging.EventHubs.Consumer;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Adds Event Hubs receive endpoints that use the Azure SDK default consumer group.</summary>
public static class EventHubEndpointConnectorExtensions
{
    /// <summary>Connects a receive endpoint using <see cref="EventHubConsumerClient.DefaultConsumerGroupName" />.</summary>
    /// <param name="connector">The running Event Hubs rider.</param>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="configure">Configures the connected receive endpoint.</param>
    /// <returns>A handle for observing readiness and stopping the connected endpoint.</returns>
    public static IHostReceiveEndpointHandle ConnectEventHubEndpoint(this IEventHubEndpointConnector connector, string eventHubName,
        Action<IRiderRegistrationContext, IEventHubReceiveEndpointConfigurator> configure)
    {
        return connector.ConnectEventHubEndpoint(eventHubName, EventHubConsumerClient.DefaultConsumerGroupName, configure);
    }
}
