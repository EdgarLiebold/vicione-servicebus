using System;
using System.Threading;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines transport-host lifecycle, topology, endpoint, rider, observer, and diagnostics operations.</summary>
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
    /// <summary>Gets the transport address represented by the host.</summary>
    Uri Address { get; }

    /// <summary>Gets the bus topology associated with the host.</summary>
    IBusTopology Topology { get; }

    /// <summary>Starts the host's configured endpoints and riders.</summary>
    /// <param name="cancellationToken">The token that cancels startup.</param>
    /// <returns>A handle that exposes aggregate readiness and controls the active host generation.</returns>
    IHostHandle Start(CancellationToken cancellationToken);

    /// <summary>Adds a configured receive endpoint to the host.</summary>
    /// <param name="endpointName">The name used to identify the endpoint within the host.</param>
    /// <param name="receiveEndpoint">The endpoint owned by the host.</param>
    void AddReceiveEndpoint(string endpointName, ReceiveEndpoint receiveEndpoint);

    /// <summary>Gets a registered rider.</summary>
    /// <param name="name">The rider's registration name.</param>
    /// <returns>The registered rider.</returns>
    IRider GetRider(string name);

    /// <summary>Adds a rider controlled by the host.</summary>
    /// <param name="name">The rider's unique registration name.</param>
    /// <param name="riderControl">The rider lifecycle controller.</param>
    void AddRider(string name, IRiderControl riderControl);

    /// <summary>Combines bus, endpoint, and rider state into one health result.</summary>
    /// <param name="busState">The current bus lifecycle state.</param>
    /// <param name="healthMessage">The diagnostic associated with the bus state.</param>
    /// <returns>The aggregate bus health result.</returns>
    BusHealthResult CheckHealth(BusState busState, string healthMessage);
}


/// <summary>Defines a transport host that creates endpoints through a transport-specific configurator.</summary>
/// <typeparam name="TEndpointConfigurator">The configurator exposed for endpoints of this transport.</typeparam>
public interface IHost<out TEndpointConfigurator> :
    IHost,
    IReceiveConnector<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
}
