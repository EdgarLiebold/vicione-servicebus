using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Uses tracked queries and optional transactions for optimistic saga concurrency.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class OptimisticSagaRepositoryLockStrategy<TSaga> :
    ISagaRepositoryLockStrategy<TSaga>
    where TSaga : class, ISaga
{
    readonly ILoadQueryExecutor<TSaga> _executor;
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    /// <summary>Initializes the optimistic saga repository transaction and query strategy.</summary>
    /// <param name="executor">The optimistic single-row loader.</param>
    /// <param name="queryCustomization">An optional transformation applied to saga queries.</param>
    /// <param name="isolationLevel">The isolation level used when transactions are enabled.</param>
    /// <param name="isTransactionEnabled">Whether repository operations create transactions.</param>
    public OptimisticSagaRepositoryLockStrategy(ILoadQueryExecutor<TSaga> executor, Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization,
        IsolationLevel isolationLevel, bool isTransactionEnabled)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _queryCustomization = queryCustomization;

        IsolationLevel = Enum.IsDefined(isolationLevel)
            ? isolationLevel
            : throw new ArgumentOutOfRangeException(nameof(isolationLevel), isolationLevel, "The transaction isolation level is not defined.");
        IsTransactionEnabled = isTransactionEnabled;
    }

    /// <summary>Gets the isolation level used when transactions are enabled.</summary>
    public IsolationLevel IsolationLevel { get; }

    /// <summary>Gets whether repository operations create transactions.</summary>
    public bool IsTransactionEnabled { get; }

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
    /// <returns>The tracked saga entity, or <see langword="null"/> when no row matches.</returns>
    public Task<TSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken)
    {
        return _executor.LoadAsync(context, correlationId, cancellationToken);
    }

    /// <summary>Creates a context that defers the filtered tracked query until transaction scope.</summary>
    /// <param name="context">The DbContext that contains the saga set.</param>
    /// <param name="query">The saga filter to execute.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The optimistic query context.</returns>
    public Task<SagaLockContext<TSaga>> CreateLockContextAsync(DbContext context, ISagaQuery<TSaga> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(query);

        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<SagaLockContext<TSaga>>(cancellationToken)
            : Task.FromResult<SagaLockContext<TSaga>>(
                new OptimisticSagaLockContext<TSaga>(context, query, cancellationToken, _queryCustomization));
    }
}
