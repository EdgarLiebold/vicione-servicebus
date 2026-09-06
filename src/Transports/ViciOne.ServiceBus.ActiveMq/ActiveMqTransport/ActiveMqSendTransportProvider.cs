using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Normalizes ActiveMQ addresses and resolves send transports for a receive endpoint.</summary>
public class ActiveMqSendTransportProvider :
    ISendTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly ActiveMqReceiveEndpointContext _context;
    readonly ISessionContextSupervisor _sessionContextSupervisor;

    /// <summary>Creates a provider backed by an ActiveMQ connection and endpoint session.</summary>
    /// <param name="connectionContextSupervisor">The broker connection supervisor.</param>
    /// <param name="context">The receive-endpoint context supplying the session supervisor.</param>
    public ActiveMqSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, ActiveMqReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
        _sessionContextSupervisor = _context.SessionContextSupervisor;
    }

    /// <summary>Normalizes a destination address against the configured ActiveMQ host.</summary>
    /// <param name="address">The destination address.</param>
    /// <returns>The canonical absolute destination URI.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return _connectionContextSupervisor.NormalizeAddress(address);
    }

    /// <summary>Gets the send transport for an ActiveMQ destination.</summary>
    /// <param name="address">The queue or topic address.</param>
    /// <param name="cancellationToken">The token used to cancel transport creation.</param>
    /// <returns>A task that produces the send transport.</returns>
    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _connectionContextSupervisor.CreateSendTransportAsync(_context, _sessionContextSupervisor, address, cancellationToken: cancellationToken);
    }
}
