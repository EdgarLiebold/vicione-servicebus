using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Defers loading the sagas until the transaction is started.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class OptimisticSagaLockContext<TSaga> :
    SagaLockContext<TSaga>
    where TSaga : class, ISaga
{
    readonly CancellationToken _cancellationToken;
    readonly DbContext _context;
    readonly ISagaQuery<TSaga> _query;
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    /// <summary>Initializes an optimistic saga lock context for a deferred saga query.</summary>
    /// <param name="context">The DbContext that contains the saga set.</param>
    /// <param name="query">The saga filter to execute after the transaction begins.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="queryCustomization">An optional transformation applied to the saga query.</param>
    public OptimisticSagaLockContext(DbContext context, ISagaQuery<TSaga> query, CancellationToken cancellationToken,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _query = query ?? throw new ArgumentNullException(nameof(query));
        _cancellationToken = cancellationToken;
        _queryCustomization = queryCustomization;
    }

    /// <summary>Loads the requested state.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The tracked saga entities selected by the query.</returns>
    public async Task<IList<TSaga>> LoadAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = cancellationToken.CanBeCanceled ? cancellationToken : _cancellationToken;
        operationCancellationToken.ThrowIfCancellationRequested();

        IQueryable<TSaga> queryable = SagaQueryCustomization.Apply(_context.Set<TSaga>(), _queryCustomization);

        List<TSaga> instances = await queryable.AsTracking()
            .Where(_query.FilterExpression)
            .ToListAsync(operationCancellationToken)
            .ConfigureAwait(false);

        return instances;
    }
}
