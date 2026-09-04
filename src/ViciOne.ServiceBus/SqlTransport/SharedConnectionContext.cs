using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a shared connection context implementation.
/// </summary>
public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress => _context.HostAddress;
    /// <summary>
    /// Gets the schema value.
    /// </summary>
    public string? Schema => _context.Schema;
    /// <summary>
    /// Gets the isolation level value.
    /// </summary>
    public IsolationLevel IsolationLevel => _context.IsolationLevel;

    /// <summary>
    /// Creates client context.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ClientContext CreateClientContext(CancellationToken cancellationToken)
    {
        return _context.CreateClientContext(cancellationToken);
    }

    /// <summary>
    /// Creates connection.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISqlTransportConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        return _context.CreateConnectionAsync(cancellationToken);
    }

    /// <summary>
    /// Performs the delay until message ready operation.
    /// </summary>
    /// <param name="queueId">The queue id value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DelayUntilMessageReadyAsync(long queueId, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        return _context.DelayUntilMessageReadyAsync(queueId, timeout, timeProvider, cancellationToken);
    }

    /// <summary>
    /// Performs the query operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="callback">The callback value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<T> QueryAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> callback, CancellationToken cancellationToken)
    {
        return _context.QueryAsync(callback, cancellationToken);
    }
}
