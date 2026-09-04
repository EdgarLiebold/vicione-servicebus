using System.Data;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

namespace ViciOne.ServiceBus;

public class EntityFrameworkOutboxOptions<TDbContext>
    where TDbContext : DbContext
{
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.RepeatableRead;
    public ILockStatementProvider LockStatementProvider { get; set; } = null!;
}
