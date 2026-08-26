#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;


    public class OptimisticLoadQueryExecutor<TSaga> :
        ILoadQueryExecutor<TSaga>
        where TSaga : class, ISaga
    {
        readonly Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;

        public OptimisticLoadQueryExecutor(Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization = null)
        {
            _queryCustomization = queryCustomization;
        }

        public Task<TSaga?> Load(DbContext dbContext, Guid correlationId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            IQueryable<TSaga> queryable = SagaQueryCustomization.Apply(dbContext.Set<TSaga>(), _queryCustomization);

            return queryable.AsTracking().SingleOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken);
        }
    }
}
