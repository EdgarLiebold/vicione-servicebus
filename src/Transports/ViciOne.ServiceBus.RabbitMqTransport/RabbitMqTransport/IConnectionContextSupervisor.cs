using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    Uri NormalizeAddress(Uri address);

    Task<ISendTransport> CreateSendTransport(RabbitMqReceiveEndpointContext receiveEndpointContext, IChannelContextSupervisor channelContextSupervisor,
        Uri address);

    Task<ISendTransport> CreatePublishTransport<T>(RabbitMqReceiveEndpointContext receiveEndpointContext, IChannelContextSupervisor channelContextSupervisor)
        where T : class;
}
