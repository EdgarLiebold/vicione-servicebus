using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Attaches a connection context to the value (shared, of course)
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
    /// <param name="context">The operation context.</param>
    /// <param name="sessionContextSupervisor">The session context supervisor value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ISendTransport> CreateSendTransportAsync(ActiveMqReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor, Uri address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates publish transport.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="sessionContextSupervisor">The session context supervisor value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ISendTransport> CreatePublishTransportAsync<T>(ActiveMqReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor, CancellationToken cancellationToken = default)
        where T : class;
}
