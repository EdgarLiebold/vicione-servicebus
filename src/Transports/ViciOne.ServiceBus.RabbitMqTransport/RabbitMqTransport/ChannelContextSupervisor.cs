// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using Transports;


    public class ChannelContextSupervisor :
        TransportPipeContextSupervisor<ChannelContext>,
        IChannelContextSupervisor
    {
        public ChannelContextSupervisor(IConnectionContextSupervisor connectionContextSupervisor, ushort? concurrentMessageLimit)
            : base(new ChannelContextFactory(connectionContextSupervisor, concurrentMessageLimit))
        {
            connectionContextSupervisor.AddConsumeAgent(this);
        }

        public ChannelContextSupervisor(IChannelContextSupervisor channelContextSupervisor)
            : base(new SharedChannelContextFactory(channelContextSupervisor))
        {
            channelContextSupervisor.AddSendAgent(this);
        }
    }
}
