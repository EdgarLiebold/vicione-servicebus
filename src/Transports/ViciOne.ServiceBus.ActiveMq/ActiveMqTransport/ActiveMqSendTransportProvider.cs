using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides an active mq send transport provider implementation.
/// </summary>
public class ActiveMqSendTransportProvider :
    ISendTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly ActiveMqReceiveEndpointContext _context;
    readonly ISessionContextSupervisor _sessionContextSupervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContextSupervisor">The connection context supervisor value.</param>
    /// <param name="context">The operation context.</param>
    public ActiveMqSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, ActiveMqReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
        _sessionContextSupervisor = _context.SessionContextSupervisor;
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
        return _connectionContextSupervisor.CreateSendTransportAsync(_context, _sessionContextSupervisor, address, cancellationToken: cancellationToken);
    }
}
