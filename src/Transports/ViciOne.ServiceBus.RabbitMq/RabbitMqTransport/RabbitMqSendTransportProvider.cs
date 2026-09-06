using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Creates RabbitMQ send transports from the endpoint's shared channel supervisor.</summary>
public class RabbitMqSendTransportProvider :
    ISendTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly IChannelContextSupervisor _channelContextSupervisor;
    readonly RabbitMqReceiveEndpointContext _receiveEndpointContext;

    /// <summary>Creates a provider bound to a connection and receive endpoint.</summary>
    /// <param name="connectionContextSupervisor">The RabbitMQ connection supervisor.</param>
    /// <param name="receiveEndpointContext">The endpoint that owns send transports.</param>
    public RabbitMqSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, RabbitMqReceiveEndpointContext receiveEndpointContext)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _channelContextSupervisor = receiveEndpointContext.ChannelContextSupervisor;
        _receiveEndpointContext = receiveEndpointContext;
    }

    /// <summary>Resolves a full or short destination address against the configured host.</summary>
    /// <param name="address">The destination address to resolve.</param>
    /// <returns>The full RabbitMQ destination URI.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return _connectionContextSupervisor.NormalizeAddress(address);
    }

    /// <summary>Gets a send transport for a RabbitMQ destination address.</summary>
    /// <param name="address">The full or short destination address.</param>
    /// <param name="cancellationToken">Cancellation checked before transport creation.</param>
    /// <returns>The configured RabbitMQ send transport.</returns>
    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _connectionContextSupervisor.CreateSendTransportAsync(_receiveEndpointContext, _channelContextSupervisor, address, cancellationToken: cancellationToken);
    }
}
