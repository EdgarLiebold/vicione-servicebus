using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

/// <summary>
/// Attaches a channel context to the value
/// </summary>
public interface IChannelContextSupervisor :
    ITransportSupervisor<ChannelContext>
{
}
