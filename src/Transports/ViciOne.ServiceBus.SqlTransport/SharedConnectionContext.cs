using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Carries state for shared connection operations.</summary>
public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the cancellation token.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the host address.</summary>
    public Uri HostAddress => _context.HostAddress;
    /// <summary>Gets the schema.</summary>
    public string? Schema => _context.Schema;
    /// <summary>Gets the isolation level.</summary>
    public IsolationLevel IsolationLevel => _context.IsolationLevel;

    /// <summary>Creates client context.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created client context.</returns>
    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return _context.CreateClientContext(cancellationToken);
    }

    /// <summary>Creates connection.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public Task<ISqlTransportConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        return _context.CreateConnectionAsync(cancellationToken);
    }

    /// <summary>Delays processing until the message becomes eligible.</summary>
    /// <param name="queueId">The queue id.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DelayUntilMessageReadyAsync(long queueId, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        return _context.DelayUntilMessageReadyAsync(queueId, timeout, timeProvider, cancellationToken);
    }

    /// <summary>Queries the configured data source.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the query outcome.</returns>
    public Task<T> QueryAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> callback, CancellationToken cancellationToken)
    {
        return _context.QueryAsync(callback, cancellationToken);
    }
}
