using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga;

public class OptimisticSagaRepositoryLockStrategy<TSaga> :
    ISagaRepositoryLockStrategy<TSaga>
    where TSaga : class, ISaga
{
    readonly ILoadQueryExecutor<TSaga> _executor;
    readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

    public OptimisticSagaRepositoryLockStrategy(ILoadQueryExecutor<TSaga> executor, Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization,
        IsolationLevel isolationLevel, bool isTransactionEnabled)
    {
        _executor = executor;
        _queryCustomization = queryCustomization;

        IsolationLevel = isolationLevel;
        IsTransactionEnabled = isTransactionEnabled;
    }

    public IsolationLevel IsolationLevel { get; }

    public bool IsTransactionEnabled { get; }

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
        return new OptimisticSagaLockContext<TSaga>(context, query, cancellationToken, _queryCustomization);
    }
}
