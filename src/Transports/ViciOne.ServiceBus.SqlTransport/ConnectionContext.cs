using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Exposes state for connection operations.</summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>Gets the host address.</summary>
    Uri HostAddress { get; }

    /// <summary>Gets the schema.</summary>
    string? Schema { get; }

    /// <summary>Gets the isolation level.</summary>
    IsolationLevel IsolationLevel { get; }

    /// <summary>Creates client context.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created client context.</returns>
    ClientContext CreateClientContext(CancellationToken cancellationToken);

    /// <summary>Create a database connection.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<ISqlTransportConnection> CreateConnectionAsync(CancellationToken cancellationToken);

    /// <summary>Delays processing until the message becomes eligible.</summary>
    /// <param name="queueId">The queue id.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DelayUntilMessageReadyAsync(long queueId, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken);

    /// <summary>Executes a query within a transaction using an available connection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the query outcome.</returns>
    Task<T> QueryAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> callback, CancellationToken cancellationToken);
}
