using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Loads a saga through a tracked EF Core query without explicit row locking.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
internal sealed class OptimisticLoadQueryExecutor<TSaga> :
    ILoadQueryExecutor<TSaga>
    where TSaga : class, ISaga
{
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    /// <summary>Initializes an optimistic saga-row loader with optional query customization.</summary>
    /// <param name="queryCustomization">An optional transformation applied before the identity predicate.</param>
    public OptimisticLoadQueryExecutor(Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization = null)
    {
        _queryCustomization = queryCustomization;
    }

    /// <summary>Loads the requested state.</summary>
    /// <param name="dbContext">The DbContext that contains the saga set.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The tracked saga entity, or <see langword="null"/> when no row matches.</returns>
    public Task<TSaga?> LoadAsync(DbContext dbContext, Guid correlationId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        IQueryable<TSaga> queryable = SagaQueryCustomization.Apply(dbContext.Set<TSaga>(), _queryCustomization);

        return queryable.AsTracking().SingleOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken);
    }
}
