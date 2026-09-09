using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Loads a saga through provider-specific row-lock SQL.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class PessimisticLoadQueryExecutor<TSaga> :
    ILoadQueryExecutor<TSaga>
    where TSaga : class, ISaga
{
    readonly ILockStatementProvider _lockStatementProvider;
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    /// <summary>Initializes a provider-specific pessimistic saga-row loader.</summary>
    /// <param name="lockStatementProvider">The provider that generates row-lock SQL from EF Core mappings.</param>
    /// <param name="queryCustomization">An optional transformation applied to the raw SQL query.</param>
    public PessimisticLoadQueryExecutor(ILockStatementProvider lockStatementProvider, Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization)
    {
        _lockStatementProvider = lockStatementProvider ?? throw new ArgumentNullException(nameof(lockStatementProvider));
        _queryCustomization = queryCustomization;
    }

    /// <summary>Loads the requested state.</summary>
    /// <param name="dbContext">The DbContext that contains the saga set.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The tracked locked saga entity, or <see langword="null"/> when no row matches.</returns>
    public Task<TSaga?> LoadAsync(DbContext dbContext, Guid correlationId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var statement = _lockStatementProvider.GetRowLockStatement<TSaga>(dbContext);

        IQueryable<TSaga> queryable = dbContext.Set<TSaga>().FromSqlRaw(statement, correlationId);

        queryable = SagaQueryCustomization.Apply(queryable, _queryCustomization);

        return queryable.AsTracking().SingleOrDefaultAsync(cancellationToken);
    }
}
