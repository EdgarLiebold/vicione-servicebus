using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>
/// Provides a pessimistic saga repository lock strategy implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class PessimisticSagaRepositoryLockStrategy<TSaga> :
    ISagaRepositoryLockStrategy<TSaga>
    where TSaga : class, ISaga
{
    readonly ILoadQueryExecutor<TSaga> _executor;
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="executor">The executor value.</param>
    /// <param name="queryCustomization">The query customization value.</param>
    /// <param name="isolationLevel">The isolation level value.</param>
    public PessimisticSagaRepositoryLockStrategy(ILoadQueryExecutor<TSaga> executor,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization, IsolationLevel isolationLevel)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _queryCustomization = queryCustomization;

        IsolationLevel = isolationLevel;
    }

    /// <summary>
    /// Gets the isolation level value.
    /// </summary>
    public IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Pessimistic concurrency always uses transactions as locks require transaction scope.
    /// </summary>
    public bool IsTransactionEnabled => true;

    /// <summary>
    /// Performs the apply query customization operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <returns>The result of the operation.</returns>
    public IQueryable<TSaga> ApplyQueryCustomization(IQueryable<TSaga> query)
    {
        return SagaQueryCustomization.Apply(query, _queryCustomization);
    }

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken)
    {
        return _executor.LoadAsync(context, correlationId, cancellationToken);
    }

    /// <summary>
    /// Creates lock context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
