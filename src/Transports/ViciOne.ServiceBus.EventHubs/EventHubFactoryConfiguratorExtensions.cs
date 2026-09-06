using System;
using Azure.Messaging.EventHubs.Consumer;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Adds Event Hubs receive endpoints that use the Azure SDK default consumer group.</summary>
public static class EventHubFactoryConfiguratorExtensions
{
    /// <summary>Configures a receive endpoint using <see cref="EventHubConsumerClient.DefaultConsumerGroupName" />.</summary>
    /// <param name="configurator">The Event Hubs rider configurator.</param>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="configure">Configures the receive endpoint.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static void ReceiveEndpoint(this IEventHubFactoryConfigurator configurator, string eventHubName,
        Action<IEventHubReceiveEndpointConfigurator> configure)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        configurator.ReceiveEndpoint(eventHubName, EventHubConsumerClient.DefaultConsumerGroupName, configure);
    }
}
