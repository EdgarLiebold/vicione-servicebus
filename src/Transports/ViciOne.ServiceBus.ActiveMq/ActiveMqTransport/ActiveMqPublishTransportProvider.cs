using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Resolves ActiveMQ publish transports for a receive endpoint.</summary>
public class ActiveMqPublishTransportProvider :
    IPublishTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly ActiveMqReceiveEndpointContext _context;
    readonly ISessionContextSupervisor _supervisor;

    /// <summary>Creates a provider backed by an ActiveMQ connection and endpoint session.</summary>
    /// <param name="connectionContextSupervisor">The broker connection supervisor.</param>
    /// <param name="context">The receive-endpoint context supplying the session supervisor.</param>
    public ActiveMqPublishTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, ActiveMqReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
        _supervisor = context.SessionContextSupervisor;
    }

    /// <summary>Gets the publish transport for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <param name="publishAddress">An optional address supplied by the caller; ActiveMQ topology determines the actual publish destination.</param>
    /// <param name="cancellationToken">The token used to cancel transport creation.</param>
    /// <returns>A task that produces the publish transport.</returns>
    public Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        return _connectionContextSupervisor.CreatePublishTransportAsync<T>(_context, _supervisor, cancellationToken: cancellationToken);
    }
}
