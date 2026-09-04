using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a channel context supervisor implementation.
/// </summary>
public class ChannelContextSupervisor :
    TransportPipeContextSupervisor<ChannelContext>,
    IChannelContextSupervisor
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContextSupervisor">The connection context supervisor value.</param>
    /// <param name="concurrentMessageLimit">The concurrent message limit value.</param>
    public ChannelContextSupervisor(IConnectionContextSupervisor connectionContextSupervisor, ushort? concurrentMessageLimit)
        : base(new ChannelContextFactory(connectionContextSupervisor, concurrentMessageLimit))
    {
        connectionContextSupervisor.AddConsumeAgent(this);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="channelContextSupervisor">The channel context supervisor value.</param>
    public ChannelContextSupervisor(IChannelContextSupervisor channelContextSupervisor)
        : base(new SharedChannelContextFactory(channelContextSupervisor))
    {
        channelContextSupervisor.AddSendAgent(this);
    }
}
