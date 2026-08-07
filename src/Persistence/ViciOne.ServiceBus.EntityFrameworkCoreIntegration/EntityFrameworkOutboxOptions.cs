// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Data;
    using EntityFrameworkCoreIntegration;
    using Microsoft.EntityFrameworkCore;


    public class EntityFrameworkOutboxOptions<TDbContext>
        where TDbContext : DbContext
    {
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.RepeatableRead;
        public ILockStatementProvider LockStatementProvider { get; set; } = new SqlServerLockStatementProvider();
    }
}
