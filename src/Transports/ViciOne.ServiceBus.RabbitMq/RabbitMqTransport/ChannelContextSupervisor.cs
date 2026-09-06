using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Supervises an owned or shared RabbitMQ channel context.</summary>
public class ChannelContextSupervisor :
    TransportPipeContextSupervisor<ChannelContext>,
    IChannelContextSupervisor
{
    /// <summary>Creates a receive-side supervisor that owns channels from a connection supervisor.</summary>
    /// <param name="connectionContextSupervisor">The supervisor that supplies RabbitMQ connections.</param>
    /// <param name="concurrentMessageLimit">The optional consumer concurrency used to size channel prefetch.</param>
    public ChannelContextSupervisor(IConnectionContextSupervisor connectionContextSupervisor, ushort? concurrentMessageLimit)
        : base(new ChannelContextFactory(connectionContextSupervisor, concurrentMessageLimit))
    {
        connectionContextSupervisor.AddConsumeAgent(this);
    }

    /// <summary>Creates a send-side supervisor that shares another channel supervisor's context.</summary>
    /// <param name="channelContextSupervisor">The supervisor that owns the shared RabbitMQ channel.</param>
    public ChannelContextSupervisor(IChannelContextSupervisor channelContextSupervisor)
        : base(new SharedChannelContextFactory(channelContextSupervisor))
    {
        channelContextSupervisor.AddSendAgent(this);
    }
}
