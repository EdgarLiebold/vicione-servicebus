using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMqTransport;

public class RabbitMqPublishTransportProvider :
    IPublishTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly IChannelContextSupervisor _supervisor;
    readonly RabbitMqReceiveEndpointContext _receiveEndpointContext;

    public RabbitMqPublishTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, RabbitMqReceiveEndpointContext receiveEndpointContext)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _supervisor = receiveEndpointContext.ChannelContextSupervisor;
        _receiveEndpointContext = receiveEndpointContext;
    }

    public Task<ISendTransport> GetPublishTransport<T>(Uri? publishAddress)
        where T : class
    {
        return _connectionContextSupervisor.CreatePublishTransport<T>(_receiveEndpointContext, _supervisor);
    }
}
