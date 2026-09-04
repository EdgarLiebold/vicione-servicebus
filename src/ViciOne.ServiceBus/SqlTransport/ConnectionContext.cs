using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

public interface ConnectionContext :
    PipeContext
{
    Uri HostAddress { get; }

    string? Schema { get; }

    IsolationLevel IsolationLevel { get; }

    ClientContext CreateClientContext(CancellationToken cancellationToken);

    /// <summary>
    /// Create a database connection
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ISqlTransportConnection> CreateConnectionAsync(CancellationToken cancellationToken);

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
