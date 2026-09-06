using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Exposes Amazon topology and client supervision for an Amazon SQS receive endpoint.</summary>
public interface SqsReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>Gets the endpoint's topics, queues, and subscriptions.</summary>
    BrokerTopology BrokerTopology { get; }

    /// <summary>Gets the supervisor for Amazon client contexts used by the endpoint.</summary>
    IClientContextSupervisor ClientContextSupervisor { get; }
}
