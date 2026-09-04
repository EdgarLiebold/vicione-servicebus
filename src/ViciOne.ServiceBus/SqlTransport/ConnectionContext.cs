using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for connection context.
/// </summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>
    /// Gets the host address value.
    /// </summary>
    Uri HostAddress { get; }

    /// <summary>
    /// Gets the schema value.
    /// </summary>
    string? Schema { get; }

    /// <summary>
    /// Gets the isolation level value.
    /// </summary>
    IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Creates client context.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ClientContext CreateClientContext(CancellationToken cancellationToken);

    /// <summary>
    /// Create a database connection
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ISqlTransportConnection> CreateConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Performs the delay until message ready operation.
    /// </summary>
    /// <param name="queueId">The queue id value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DelayUntilMessageReadyAsync(long queueId, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a query within a transaction using an available connection
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> QueryAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> callback, CancellationToken cancellationToken);
}
