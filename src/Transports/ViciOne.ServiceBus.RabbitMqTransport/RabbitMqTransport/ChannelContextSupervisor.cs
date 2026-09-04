using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

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
