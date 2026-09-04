using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides an active mq publish transport provider implementation.
/// </summary>
public class ActiveMqPublishTransportProvider :
    IPublishTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly ActiveMqReceiveEndpointContext _context;
    readonly ISessionContextSupervisor _supervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContextSupervisor">The connection context supervisor value.</param>
    /// <param name="context">The operation context.</param>
    public ActiveMqPublishTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, ActiveMqReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
        _supervisor = context.SessionContextSupervisor;
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
        return _connectionContextSupervisor.CreatePublishTransportAsync<T>(_context, _supervisor, cancellationToken: cancellationToken);
    }
}
