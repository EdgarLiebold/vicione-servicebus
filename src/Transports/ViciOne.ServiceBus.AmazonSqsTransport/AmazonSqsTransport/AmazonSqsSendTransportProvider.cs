using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public class AmazonSqsSendTransportProvider :
    ISendTransportProvider
{
    readonly IClientContextSupervisor _clientContextSupervisor;
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly SqsReceiveEndpointContext _context;

    public AmazonSqsSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, SqsReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
        _clientContextSupervisor = context.ClientContextSupervisor;
    }

    public Uri NormalizeAddress(Uri address)
    {
        return _connectionContextSupervisor.NormalizeAddress(address);
    }

    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _connectionContextSupervisor.CreateSendTransportAsync(_context, _clientContextSupervisor, address, cancellationToken: cancellationToken);
    }
}
