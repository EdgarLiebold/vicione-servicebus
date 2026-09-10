using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Exposes state for sql receive endpoint operations.</summary>
public interface SqlReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>Gets the client context supervisor.</summary>
    IClientContextSupervisor ClientContextSupervisor { get; }

    /// <summary>Gets the broker topology.</summary>
    BrokerTopology BrokerTopology { get; }
}
