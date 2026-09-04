using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>
/// Defers loading the sagas until the transaction is started
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public class OptimisticSagaLockContext<TSaga> :
    SagaLockContext<TSaga>
    where TSaga : class, ISaga
{
    readonly CancellationToken _cancellationToken;
    readonly DbContext _context;
    readonly ISagaQuery<TSaga> _query;
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="queryCustomization">The query customization value.</param>
    public OptimisticSagaLockContext(DbContext context, ISagaQuery<TSaga> query, CancellationToken cancellationToken,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization)
    {
        _context = context;
        _query = query;
        _cancellationToken = cancellationToken;
        _queryCustomization = queryCustomization;
    }

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<IList<TSaga>> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); IQueryable<TSaga> queryable = SagaQueryCustomization.Apply(_context.Set<TSaga>(), _queryCustomization);

        List<TSaga> instances = await queryable.AsTracking()
            .Where(_query.FilterExpression)
            .ToListAsync(_cancellationToken)
            .ConfigureAwait(false);

        return instances;
    }
}
