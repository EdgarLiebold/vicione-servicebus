using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq receive endpoint context.
/// </summary>
public interface ActiveMqReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>
    /// Gets the broker topology value.
    /// </summary>
    BrokerTopology BrokerTopology { get; }

    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>
    /// Gets the session context supervisor value.
    /// </summary>
    ISessionContextSupervisor SessionContextSupervisor { get; }
}
