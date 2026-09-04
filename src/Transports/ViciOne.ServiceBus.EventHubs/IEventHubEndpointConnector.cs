using System;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for event hub endpoint connector.
/// </summary>
public interface IEventHubEndpointConnector
{
    /// <summary>
    /// Connects event hub endpoint.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="consumerGroup">The consumer group value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    HostReceiveEndpointHandle ConnectEventHubEndpoint(string eventHubName, string consumerGroup,
        Action<IRiderRegistrationContext, IEventHubReceiveEndpointConfigurator> configure);
}
