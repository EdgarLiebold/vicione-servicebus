using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Creates Amazon SQS send transports for a receive endpoint.</summary>
public class AmazonSqsSendTransportProvider :
    ISendTransportProvider
{
    readonly IClientContextSupervisor _clientContextSupervisor;
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly SqsReceiveEndpointContext _context;

    /// <summary>Initializes an Amazon SQS send-transport provider.</summary>
    /// <param name="connectionContextSupervisor">The supervisor used to normalize addresses and create connection-scoped transports.</param>
    /// <param name="context">The receive-endpoint context that owns the client supervisor.</param>
    public AmazonSqsSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, SqsReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
        _clientContextSupervisor = context.ClientContextSupervisor;
    }

    /// <summary>Resolves an endpoint address relative to the configured Amazon SQS host.</summary>
    /// <param name="address">The absolute or relative endpoint address.</param>
    /// <returns>The normalized absolute endpoint address.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return _connectionContextSupervisor.NormalizeAddress(address);
    }

    /// <summary>Gets the Amazon SQS transport for an endpoint address.</summary>
    /// <param name="address">The queue endpoint address.</param>
    /// <param name="cancellationToken">The token used to cancel transport creation.</param>
    /// <returns>The queue send transport.</returns>
    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _connectionContextSupervisor.CreateSendTransportAsync(_context, _clientContextSupervisor, address, cancellationToken: cancellationToken);
    }
}
