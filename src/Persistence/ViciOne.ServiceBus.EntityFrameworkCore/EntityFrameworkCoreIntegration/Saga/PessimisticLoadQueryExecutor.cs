using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>
/// Provides a pessimistic load query executor implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class PessimisticLoadQueryExecutor<TSaga> :
    ILoadQueryExecutor<TSaga>
    where TSaga : class, ISaga
{
    readonly ILockStatementProvider _lockStatementProvider;
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="lockStatementProvider">The lock statement provider value.</param>
    /// <param name="queryCustomization">The query customization value.</param>
    public PessimisticLoadQueryExecutor(ILockStatementProvider lockStatementProvider, Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization)
    {
        _lockStatementProvider = lockStatementProvider ?? throw new ArgumentNullException(nameof(lockStatementProvider));
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

        var statement = _lockStatementProvider.GetRowLockStatement<TSaga>(dbContext);

        IQueryable<TSaga> queryable = dbContext.Set<TSaga>().FromSqlRaw(statement, correlationId);

        queryable = SagaQueryCustomization.Apply(queryable, _queryCustomization);

        return queryable.AsTracking().SingleOrDefaultAsync(cancellationToken);
    }
}
