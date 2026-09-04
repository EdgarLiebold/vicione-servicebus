using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for sqs receive endpoint context.
/// </summary>
public interface SqsReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>
    /// Gets the broker topology value.
    /// </summary>
    BrokerTopology BrokerTopology { get; }

    /// <summary>
    /// Gets the client context supervisor value.
    /// </summary>
    IClientContextSupervisor ClientContextSupervisor { get; }
}
