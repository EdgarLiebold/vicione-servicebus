using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for sql receive endpoint context.
/// </summary>
public interface SqlReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>
    /// Gets the client context supervisor value.
    /// </summary>
    IClientContextSupervisor ClientContextSupervisor { get; }

    /// <summary>
    /// Gets the broker topology value.
    /// </summary>
    BrokerTopology BrokerTopology { get; }
}
