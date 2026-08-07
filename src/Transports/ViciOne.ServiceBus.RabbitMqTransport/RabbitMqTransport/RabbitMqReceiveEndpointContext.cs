// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using Topology;
    using Transports;


    public interface RabbitMqReceiveEndpointContext :
        ReceiveEndpointContext
    {
        BrokerTopology BrokerTopology { get; }

        bool ExclusiveConsumer { get; }

        bool IsNotReplyTo { get; }

        IChannelContextSupervisor ChannelContextSupervisor { get; }
    }
}
