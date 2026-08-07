// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkIntegration.Saga
{
    using System;
    using System.Data.Entity;
    using System.Threading;
    using System.Threading.Tasks;


    public class OptimisticLoadQueryExecutor<TSaga> :
        ILoadQueryExecutor<TSaga>
        where TSaga : class, ISaga
    {
        readonly ILoadQueryProvider<TSaga> _provider;

        public OptimisticLoadQueryExecutor(ILoadQueryProvider<TSaga> provider)
        {
            _provider = provider;
        }

        public Task<TSaga> Load(DbContext dbContext, Guid correlationId, CancellationToken cancellationToken)
        {
            return _provider.GetQueryable(dbContext).SingleOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken);
        }
    }
}
