using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Supervises RabbitMQ channel availability and dependent transport agents.</summary>
public interface IChannelContextSupervisor :
    ITransportSupervisor<ChannelContext>
{
}
