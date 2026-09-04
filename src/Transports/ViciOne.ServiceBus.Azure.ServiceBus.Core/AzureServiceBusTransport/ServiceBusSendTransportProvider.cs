using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ServiceBusSendTransportProvider :
    ISendTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly ReceiveEndpointContext _context;

    public ServiceBusSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, ReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
    }

    public Uri NormalizeAddress(Uri address)
    {
        return _connectionContextSupervisor.NormalizeAddress(address);
    }

    Task<ISendTransport> ISendTransportProvider.GetSendTransportAsync(Uri address, CancellationToken cancellationToken)
    {
        return _connectionContextSupervisor.CreateSendTransportAsync(_context, address, cancellationToken: cancellationToken);
    }
}
