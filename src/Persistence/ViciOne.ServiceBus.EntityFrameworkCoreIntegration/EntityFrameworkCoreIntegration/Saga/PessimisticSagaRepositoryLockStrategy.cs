using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga;

public class PessimisticSagaRepositoryLockStrategy<TSaga> :
    ISagaRepositoryLockStrategy<TSaga>
    where TSaga : class, ISaga
{
    readonly ILoadQueryExecutor<TSaga> _executor;
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    public PessimisticSagaRepositoryLockStrategy(ILoadQueryExecutor<TSaga> executor,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization, IsolationLevel isolationLevel)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _queryCustomization = queryCustomization;

        IsolationLevel = isolationLevel;
    }

    public IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Pessimistic concurrency always uses transactions as locks require transaction scope.
    /// </summary>
    public bool IsTransactionEnabled => true;

    public IQueryable<TSaga> ApplyQueryCustomization(IQueryable<TSaga> query)
    {
        return SagaQueryCustomization.Apply(query, _queryCustomization);
    }

    public Task<TSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken)
    {
        return _executor.LoadAsync(context, correlationId, cancellationToken);
    }

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
