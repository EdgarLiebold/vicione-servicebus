using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    public override CancellationToken CancellationToken { get; }

    public Uri HostAddress => _context.HostAddress;
    public string? Schema => _context.Schema;
    public IsolationLevel IsolationLevel => _context.IsolationLevel;

    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return _context.CreateClientContext(cancellationToken);
    }

    public Task<ISqlTransportConnection> CreateConnection(CancellationToken cancellationToken)
    {
        return _context.CreateConnection(cancellationToken);
    }

    public Task DelayUntilMessageReady(long queueId, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        return _context.DelayUntilMessageReady(queueId, timeout, timeProvider, cancellationToken);
    }

    public Task<T> Query<T>(Func<IDbConnection, IDbTransaction, Task<T>> callback, CancellationToken cancellationToken)
    {
        return _context.Query(callback, cancellationToken);
    }
}
