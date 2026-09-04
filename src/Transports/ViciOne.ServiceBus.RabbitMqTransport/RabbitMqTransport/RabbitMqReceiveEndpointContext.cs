using ViciOne.ServiceBus.RabbitMqTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public interface RabbitMqReceiveEndpointContext :
    ReceiveEndpointContext
{
    BrokerTopology BrokerTopology { get; }

    bool ExclusiveConsumer { get; }

    bool IsNotReplyTo { get; }

    IChannelContextSupervisor ChannelContextSupervisor { get; }
}
