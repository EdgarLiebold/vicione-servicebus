using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq send transport provider implementation.
/// </summary>
public class RabbitMqSendTransportProvider :
    ISendTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly IChannelContextSupervisor _channelContextSupervisor;
    readonly RabbitMqReceiveEndpointContext _receiveEndpointContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContextSupervisor">The connection context supervisor value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    public RabbitMqSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, RabbitMqReceiveEndpointContext receiveEndpointContext)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _channelContextSupervisor = receiveEndpointContext.ChannelContextSupervisor;
        _receiveEndpointContext = receiveEndpointContext;
    }

    /// <summary>
    /// Performs the normalize address operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return _connectionContextSupervisor.NormalizeAddress(address);
    }

    /// <summary>
    /// Gets send transport.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _connectionContextSupervisor.CreateSendTransportAsync(_receiveEndpointContext, _channelContextSupervisor, address, cancellationToken: cancellationToken);
    }
}
