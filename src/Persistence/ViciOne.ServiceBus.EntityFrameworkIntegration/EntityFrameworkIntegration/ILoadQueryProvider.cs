// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkIntegration
{
    using System.Data.Entity;
    using System.Linq;


    public interface ILoadQueryProvider<out TSaga>
        where TSaga : class, ISaga
    {
        IQueryable<TSaga> GetQueryable(DbContext dbContext);
    }
}
