using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides an amazon sqs publish transport provider implementation.
/// </summary>
public class AmazonSqsPublishTransportProvider :
    IPublishTransportProvider
{
    readonly IClientContextSupervisor _clientContextSupervisor;
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly SqsReceiveEndpointContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContextSupervisor">The connection context supervisor value.</param>
    /// <param name="context">The operation context.</param>
    public AmazonSqsPublishTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, SqsReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
        _clientContextSupervisor = context.ClientContextSupervisor;
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
        return _connectionContextSupervisor.CreatePublishTransportAsync<T>(_context, _clientContextSupervisor, cancellationToken: cancellationToken);
    }
}
