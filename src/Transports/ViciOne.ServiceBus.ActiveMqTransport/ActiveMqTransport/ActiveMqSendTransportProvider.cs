using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ActiveMqSendTransportProvider :
    ISendTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly ActiveMqReceiveEndpointContext _context;
    readonly ISessionContextSupervisor _sessionContextSupervisor;

    public ActiveMqSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, ActiveMqReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
        _sessionContextSupervisor = _context.SessionContextSupervisor;
    }

    public Uri NormalizeAddress(Uri address)
    {
        return _connectionContextSupervisor.NormalizeAddress(address);
    }

    public Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _connectionContextSupervisor.CreateSendTransportAsync(_context, _sessionContextSupervisor, address, cancellationToken: cancellationToken);
    }
}
