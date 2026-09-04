using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public class RabbitMqSendTransportProvider :
    ISendTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly IChannelContextSupervisor _channelContextSupervisor;
    readonly RabbitMqReceiveEndpointContext _receiveEndpointContext;

    public RabbitMqSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, RabbitMqReceiveEndpointContext receiveEndpointContext)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _channelContextSupervisor = receiveEndpointContext.ChannelContextSupervisor;
        _receiveEndpointContext = receiveEndpointContext;
    }

    public Uri NormalizeAddress(Uri address)
    {
        return _connectionContextSupervisor.NormalizeAddress(address);
    }

    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _connectionContextSupervisor.CreateSendTransportAsync(_receiveEndpointContext, _channelContextSupervisor, address, cancellationToken: cancellationToken);
    }
}
