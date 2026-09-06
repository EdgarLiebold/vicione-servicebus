using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Creates RabbitMQ publish transports from the endpoint's shared channel supervisor.</summary>
public class RabbitMqPublishTransportProvider :
    IPublishTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly IChannelContextSupervisor _supervisor;
    readonly RabbitMqReceiveEndpointContext _receiveEndpointContext;

    /// <summary>Creates a provider bound to a connection and receive endpoint.</summary>
    /// <param name="connectionContextSupervisor">The RabbitMQ connection supervisor.</param>
    /// <param name="receiveEndpointContext">The endpoint that owns publish transports.</param>
    public RabbitMqPublishTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, RabbitMqReceiveEndpointContext receiveEndpointContext)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _supervisor = receiveEndpointContext.ChannelContextSupervisor;
        _receiveEndpointContext = receiveEndpointContext;
    }

    /// <summary>Gets the publish transport selected by message-contract topology.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="publishAddress">Ignored because RabbitMQ publish topology derives the exchange address.</param>
    /// <param name="cancellationToken">Cancellation checked before transport creation.</param>
    /// <returns>The configured RabbitMQ publish transport.</returns>
    public Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        return _connectionContextSupervisor.CreatePublishTransportAsync<T>(_receiveEndpointContext, _supervisor, cancellationToken: cancellationToken);
    }
}
