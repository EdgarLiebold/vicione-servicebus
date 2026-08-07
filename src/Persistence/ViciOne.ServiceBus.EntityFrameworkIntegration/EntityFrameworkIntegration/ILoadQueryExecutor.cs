// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkIntegration
{
    using System;
    using System.Data.Entity;
    using System.Threading;
    using System.Threading.Tasks;


    public interface ILoadQueryExecutor<TSaga>
        where TSaga : class, ISaga
    {
        Task<TSaga> Load(DbContext dbContext, Guid correlationId, CancellationToken cancellationToken);
    }
}
