using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    Uri NormalizeAddress(Uri address);

    Task<ISendTransport> CreateSendTransportAsync(RabbitMqReceiveEndpointContext receiveEndpointContext, IChannelContextSupervisor channelContextSupervisor,
        Uri address, CancellationToken cancellationToken = default);

    Task<ISendTransport> CreatePublishTransportAsync<T>(RabbitMqReceiveEndpointContext receiveEndpointContext, IChannelContextSupervisor channelContextSupervisor, CancellationToken cancellationToken = default)
        where T : class;
}
