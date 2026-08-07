// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using Transports;


    /// <summary>
    /// Attaches a channel context to the value
    /// </summary>
    public interface IChannelContextSupervisor :
        ITransportSupervisor<ChannelContext>
    {
    }
}
