using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Uses provider row locks inside transactions for pessimistic saga concurrency.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class PessimisticSagaRepositoryLockStrategy<TSaga> :
    ISagaRepositoryLockStrategy<TSaga>
    where TSaga : class, ISaga
{
    readonly ILoadQueryExecutor<TSaga> _executor;
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    /// <summary>Initializes the pessimistic saga repository transaction and query strategy.</summary>
    /// <param name="executor">The provider-specific single-row locking loader.</param>
    /// <param name="queryCustomization">An optional transformation applied to saga queries.</param>
    /// <param name="isolationLevel">The isolation level used by repository transactions.</param>
    public PessimisticSagaRepositoryLockStrategy(ILoadQueryExecutor<TSaga> executor,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization, IsolationLevel isolationLevel)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _queryCustomization = queryCustomization;

        IsolationLevel = isolationLevel;
    }

    /// <summary>Gets the isolation level used by repository transactions.</summary>
    public IsolationLevel IsolationLevel { get; }

    /// <summary>Pessimistic concurrency always uses transactions as locks require transaction scope.</summary>
    public bool IsTransactionEnabled => true;

    /// <summary>Applies the configured saga-query transformation.</summary>
    /// <param name="query">The base saga query.</param>
    /// <returns>The transformed query.</returns>
    public IQueryable<TSaga> ApplyQueryCustomization(IQueryable<TSaga> query)
    {
        return SagaQueryCustomization.Apply(query, _queryCustomization);
    }

    /// <summary>Loads the requested state.</summary>
    /// <param name="context">The DbContext that contains the saga set.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The tracked locked saga entity, or <see langword="null"/> when no row matches.</returns>
    public Task<TSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken)
    {
        return _executor.LoadAsync(context, correlationId, cancellationToken);
    }

    /// <summary>Selects matching saga identifiers before returning a context that locks each row individually.</summary>
    /// <param name="context">The DbContext that contains the saga set.</param>
    /// <param name="query">The saga filter used to select correlation identifiers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A context that locks and loads the selected saga rows.</returns>
    public async Task<SagaLockContext<TSaga>> CreateLockContextAsync(DbContext context, ISagaQuery<TSaga> query, CancellationToken cancellationToken)
    {
        IList<Guid> instances = await ApplyQueryCustomization(context.Set<TSaga>())
            .AsNoTracking()
            .Where(query.FilterExpression)
            .Select(x => x.CorrelationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PessimisticSagaLockContext<TSaga>(context, cancellationToken, instances, _executor);
    }
}
