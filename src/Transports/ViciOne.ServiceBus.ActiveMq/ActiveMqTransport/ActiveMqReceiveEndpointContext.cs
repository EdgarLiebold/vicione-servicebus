using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes ActiveMQ-specific runtime services for a receive endpoint.</summary>
public interface ActiveMqReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>Gets the topology deployed for the receive endpoint.</summary>
    BrokerTopology BrokerTopology { get; }

    /// <summary>Gets the broker connection supervisor.</summary>
    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    /// <summary>Gets the Apache NMS session supervisor.</summary>
    ISessionContextSupervisor SessionContextSupervisor { get; }
}
