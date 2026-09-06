using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>
/// Provides an optimistic load query executor implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class OptimisticLoadQueryExecutor<TSaga> :
    ILoadQueryExecutor<TSaga>
    where TSaga : class, ISaga
{
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queryCustomization">The query customization value.</param>
    public OptimisticLoadQueryExecutor(Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization = null)
    {
        _queryCustomization = queryCustomization;
    }

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="dbContext">The db context value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TSaga?> LoadAsync(DbContext dbContext, Guid correlationId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        IQueryable<TSaga> queryable = SagaQueryCustomization.Apply(dbContext.Set<TSaga>(), _queryCustomization);

        return queryable.AsTracking().SingleOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken);
    }
}
