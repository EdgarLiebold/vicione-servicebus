using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Creates Amazon SNS publish transports for a receive endpoint.</summary>
public class AmazonSqsPublishTransportProvider :
    IPublishTransportProvider
{
    readonly IClientContextSupervisor _clientContextSupervisor;
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly SqsReceiveEndpointContext _context;

    /// <summary>Initializes an Amazon SNS publish-transport provider.</summary>
    /// <param name="connectionContextSupervisor">The supervisor used to create connection-scoped transports.</param>
    /// <param name="context">The receive-endpoint context that owns the client supervisor.</param>
    public AmazonSqsPublishTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, SqsReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
        _clientContextSupervisor = context.ClientContextSupervisor;
    }

    /// <summary>Gets the Amazon SNS transport used to publish a message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="publishAddress">An optional address override supplied by the publish operation.</param>
    /// <param name="cancellationToken">The token used to cancel transport creation.</param>
    /// <returns>The publish send transport.</returns>
    public Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        return _connectionContextSupervisor.CreatePublishTransportAsync<T>(_context, _clientContextSupervisor, cancellationToken: cancellationToken);
    }
}
