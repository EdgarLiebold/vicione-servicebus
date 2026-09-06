using System;
using System.Threading;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by host.</summary>
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
    /// <summary>Gets the address.</summary>
    Uri Address { get; }

    /// <summary>Gets the topology.</summary>
    IBusTopology Topology { get; }

    /// <summary>Starts the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The host handle produced by the operation.</returns>
    HostHandle Start(CancellationToken cancellationToken);

    /// <summary>Adds receive endpoint to the configuration.</summary>
    /// <param name="endpointName">The endpoint name.</param>
    /// <param name="receiveEndpoint">The receive endpoint.</param>
    void AddReceiveEndpoint(string endpointName, ReceiveEndpoint receiveEndpoint);

    /// <summary>Gets rider.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The rider.</returns>
    IRider GetRider(string name);

    /// <summary>Adds rider to the configuration.</summary>
    /// <param name="name">The name.</param>
    /// <param name="riderControl">The rider control.</param>
    void AddRider(string name, IRiderControl riderControl);

    /// <summary>Checks health.</summary>
    /// <param name="busState">The bus state.</param>
    /// <param name="healthMessage">The health message.</param>
    /// <returns>The bus health result produced by the operation.</returns>
    BusHealthResult CheckHealth(BusState busState, string healthMessage);
}


/// <summary>Defines the operations required by host.</summary>
/// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
public interface IHost<out TEndpointConfigurator> :
    IHost,
    IReceiveConnector<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
}
