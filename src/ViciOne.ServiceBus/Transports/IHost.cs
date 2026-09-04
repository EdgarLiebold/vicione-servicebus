using System;
using System.Threading;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for host.
/// </summary>
public interface IHost :
    IReceiveConnector,
    IConsumeMessageObserverConnector,
    IConsumeObserverConnector,
    IReceiveObserverConnector,
    IPublishObserverConnector,
    ISendObserverConnector,
    IReceiveEndpointObserverConnector,
    IProbeSite
{
    /// <summary>
    /// Gets the address value.
    /// </summary>
    Uri Address { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    IBusTopology Topology { get; }

    /// <summary>
    /// Starts the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    HostHandle Start(CancellationToken cancellationToken);

    /// <summary>
    /// Adds receive endpoint to the configuration.
    /// </summary>
    /// <param name="endpointName">The endpoint name value.</param>
    /// <param name="receiveEndpoint">The receive endpoint value.</param>
    void AddReceiveEndpoint(string endpointName, ReceiveEndpoint receiveEndpoint);

    /// <summary>
    /// Gets rider.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the operation.</returns>
    IRider GetRider(string name);

    /// <summary>
    /// Adds rider to the configuration.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="riderControl">The rider control value.</param>
    void AddRider(string name, IRiderControl riderControl);

    /// <summary>
    /// Performs the check health operation.
    /// </summary>
    /// <param name="busState">The bus state value.</param>
    /// <param name="healthMessage">The health message value.</param>
    /// <returns>The result of the operation.</returns>
    BusHealthResult CheckHealth(BusState busState, string healthMessage);
}


/// <summary>
/// Defines the contract for host.
/// </summary>
/// <typeparam name="TEndpointConfigurator">The t endpoint configurator type.</typeparam>
public interface IHost<out TEndpointConfigurator> :
    IHost,
    IReceiveConnector<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
}
