using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq receive endpoint context.
/// </summary>
public interface RabbitMqReceiveEndpointContext :
    ReceiveEndpointContext
{
    /// <summary>
    /// Gets the broker topology value.
    /// </summary>
    BrokerTopology BrokerTopology { get; }

    /// <summary>
    /// Gets the exclusive consumer value.
    /// </summary>
    bool ExclusiveConsumer { get; }

    /// <summary>
    /// Gets the is not reply to value.
    /// </summary>
    bool IsNotReplyTo { get; }

    /// <summary>
    /// Gets the channel context supervisor value.
    /// </summary>
    IChannelContextSupervisor ChannelContextSupervisor { get; }
}
