using System;
using Azure.Messaging.EventHubs.Consumer;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides extension methods for event hub endpoint connector.
/// </summary>
public static class EventHubEndpointConnectorExtensions
{
    /// <summary>
    /// Connect to EventHub using default consumer group <see cref="EventHubConsumerClient.DefaultConsumerGroupName" />
    /// </summary>
    /// <param name="connector"></param>
    /// <param name="eventHubName">Event Hub name</param>
    /// <param name="configure"></param>
    public static HostReceiveEndpointHandle ConnectEventHubEndpoint(this IEventHubEndpointConnector connector, string eventHubName,
        Action<IRiderRegistrationContext, IEventHubReceiveEndpointConfigurator> configure)
    {
        return connector.ConnectEventHubEndpoint(eventHubName, EventHubConsumerClient.DefaultConsumerGroupName, configure);
    }
}
