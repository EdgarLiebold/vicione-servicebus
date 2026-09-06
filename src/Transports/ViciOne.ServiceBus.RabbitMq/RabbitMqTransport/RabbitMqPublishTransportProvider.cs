using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq publish transport provider implementation.
/// </summary>
public class RabbitMqPublishTransportProvider :
    IPublishTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly IChannelContextSupervisor _supervisor;
    readonly RabbitMqReceiveEndpointContext _receiveEndpointContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContextSupervisor">The connection context supervisor value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    public RabbitMqPublishTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, RabbitMqReceiveEndpointContext receiveEndpointContext)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _supervisor = receiveEndpointContext.ChannelContextSupervisor;
        _receiveEndpointContext = receiveEndpointContext;
    }

    /// <summary>
    /// Gets publish transport.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="publishAddress">The publish address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        return _connectionContextSupervisor.CreatePublishTransportAsync<T>(_receiveEndpointContext, _supervisor, cancellationToken: cancellationToken);
    }
}
