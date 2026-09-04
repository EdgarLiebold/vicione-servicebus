using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

public class SqlPublishTransportProvider :
    IPublishTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly SqlReceiveEndpointContext _context;

    public SqlPublishTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, SqlReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
    }

    public Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        return _connectionContextSupervisor.CreatePublishTransportAsync<T>(_context, publishAddress, cancellationToken: cancellationToken);
    }
}
