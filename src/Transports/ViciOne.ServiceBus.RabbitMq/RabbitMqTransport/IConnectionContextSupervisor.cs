using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for connection context supervisor.
/// </summary>
public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    /// <summary>
    /// Performs the normalize address operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    Uri NormalizeAddress(Uri address);

    /// <summary>
    /// Creates send transport.
    /// </summary>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="channelContextSupervisor">The channel context supervisor value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ISendTransport> CreateSendTransportAsync(RabbitMqReceiveEndpointContext receiveEndpointContext, IChannelContextSupervisor channelContextSupervisor,
        Uri address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates publish transport.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="channelContextSupervisor">The channel context supervisor value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ISendTransport> CreatePublishTransportAsync<T>(RabbitMqReceiveEndpointContext receiveEndpointContext, IChannelContextSupervisor channelContextSupervisor, CancellationToken cancellationToken = default)
        where T : class;
}
