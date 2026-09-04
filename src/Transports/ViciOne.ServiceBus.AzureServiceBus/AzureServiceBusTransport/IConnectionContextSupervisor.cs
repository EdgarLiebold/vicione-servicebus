using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for connection context supervisor.
/// </summary>
public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    /// <summary>
    /// Creates client context supervisor.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    IClientContextSupervisor CreateClientContextSupervisor(Func<IConnectionContextSupervisor, IPipeContextFactory<ClientContext>> factory);

    /// <summary>
    /// Creates send endpoint context supervisor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    ISendEndpointContextSupervisor CreateSendEndpointContextSupervisor(SendSettings settings);

    /// <summary>
    /// Creates send transport.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext context, Uri address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates publish transport.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="publishAddress">The publish address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext context, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the normalize address operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    Uri NormalizeAddress(Uri address);
}
