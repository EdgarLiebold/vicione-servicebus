#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using EntityFrameworkCoreIntegration;
    using Microsoft.EntityFrameworkCore;


    public class PessimisticLoadQueryExecutor<TSaga> :
        ILoadQueryExecutor<TSaga>
        where TSaga : class, ISaga
    {
        readonly ILockStatementProvider _lockStatementProvider;
        readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

        public PessimisticLoadQueryExecutor(ILockStatementProvider lockStatementProvider, Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization)
        {
            _lockStatementProvider = lockStatementProvider ?? throw new ArgumentNullException(nameof(lockStatementProvider));
            _queryCustomization = queryCustomization;
        }

        public Task<TSaga?> Load(DbContext dbContext, Guid correlationId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            var statement = _lockStatementProvider.GetRowLockStatement<TSaga>(dbContext);

            IQueryable<TSaga> queryable = dbContext.Set<TSaga>().FromSqlRaw(statement, correlationId);

            queryable = SagaQueryCustomization.Apply(queryable, _queryCustomization);

            return queryable.AsTracking().SingleOrDefaultAsync(cancellationToken);
        }
    }
}
