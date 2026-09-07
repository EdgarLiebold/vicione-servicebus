using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Queries the list of saga ids prior to the transaction, and then loads/locks them individually.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
internal sealed class PessimisticSagaLockContext<TSaga> :
    SagaLockContext<TSaga>
    where TSaga : class, ISaga
{
    readonly CancellationToken _cancellationToken;
    readonly DbContext _context;
    readonly ILoadQueryExecutor<TSaga> _executor;
    readonly IList<Guid> _instances;

    /// <summary>Initializes a pessimistic saga lock context for preselected correlation identifiers.</summary>
    /// <param name="context">The DbContext that contains the saga set.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="instances">The correlation identifiers selected before row locking.</param>
    /// <param name="executor">The loader that locks and returns each saga row.</param>
    public PessimisticSagaLockContext(DbContext context, CancellationToken cancellationToken, IList<Guid> instances, ILoadQueryExecutor<TSaga> executor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cancellationToken = cancellationToken;
        _instances = instances ?? throw new ArgumentNullException(nameof(instances));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <summary>Loads the requested state.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The saga rows that still exist when individually locked.</returns>
    public async Task<IList<TSaga>> LoadAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = cancellationToken.CanBeCanceled ? cancellationToken : _cancellationToken;
        operationCancellationToken.ThrowIfCancellationRequested();

        var loaded = new List<TSaga>();

        foreach (var correlationId in _instances)
        {
            var result = await _executor.LoadAsync(_context, correlationId, operationCancellationToken).ConfigureAwait(false);
            if (result != null)
                loaded.Add(result);
        }

        return loaded;
    }
}
