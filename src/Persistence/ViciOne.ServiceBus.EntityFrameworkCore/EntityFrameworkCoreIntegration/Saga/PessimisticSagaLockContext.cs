using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>
/// Queries the list of saga ids prior to the transaction, and then loads/locks them individually
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public class PessimisticSagaLockContext<TSaga> :
    SagaLockContext<TSaga>
    where TSaga : class, ISaga
{
    readonly CancellationToken _cancellationToken;
    readonly DbContext _context;
    readonly ILoadQueryExecutor<TSaga> _executor;
    readonly IList<Guid> _instances;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="instances">The instances value.</param>
    /// <param name="executor">The executor value.</param>
    public PessimisticSagaLockContext(DbContext context, CancellationToken cancellationToken, IList<Guid> instances, ILoadQueryExecutor<TSaga> executor)
    {
        _context = context;
        _cancellationToken = cancellationToken;
        _instances = instances;
        _executor = executor;
    }

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<IList<TSaga>> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var loaded = new List<TSaga>();

        foreach (var correlationId in _instances)
        {
            var result = await _executor.LoadAsync(_context, correlationId, _cancellationToken).ConfigureAwait(false);
            if (result != null)
                loaded.Add(result);
        }

        return loaded;
    }
}
