using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Defines the contract for event hub host configuration.
/// </summary>
public interface IEventHubHostConfiguration :
    ISpecification
{
    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>
    /// Creates send transport context.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="busInstance">The bus instance value.</param>
    /// <returns>The result of the operation.</returns>
    EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance);

    /// <summary>
    /// Creates specification.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="consumerGroup">The consumer group value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IEventHubReceiveEndpointSpecification CreateSpecification(string eventHubName, string consumerGroup,
        Action<IEventHubReceiveEndpointConfigurator> configure);

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="busInstance">The bus instance value.</param>
    /// <returns>The result of the operation.</returns>
    IEventHubRider Build(IRiderRegistrationContext context, IBusInstance busInstance);
}
