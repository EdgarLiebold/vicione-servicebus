using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Exposes RabbitMQ topology and channel supervision for a receive endpoint.</summary>
public interface RabbitMqReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>Gets the broker topology.</summary>
    BrokerTopology BrokerTopology { get; }

    /// <summary>Gets whether the broker permits only this consumer on the queue.</summary>
    bool ExclusiveConsumer { get; }

    /// <summary>Gets whether the endpoint is a normal queue rather than the direct-reply-to pseudo-queue.</summary>
    bool IsNotReplyTo { get; }

    /// <summary>Gets the channel context supervisor.</summary>
    IChannelContextSupervisor ChannelContextSupervisor { get; }
}
