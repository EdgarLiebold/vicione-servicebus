using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga;

public class DelegateSagaDbContextFactory<TSaga> :
    ISagaDbContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly Func<DbContext> _dbContextFactory;

    public DelegateSagaDbContextFactory(Func<DbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public DbContext Create()
    {
        return _dbContextFactory();
    }

    public DbContext CreateScoped<T>(ConsumeContext<T> context)
        where T : class
    {
        return _dbContextFactory();
    }

    public ValueTask ReleaseAsync(DbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); return dbContext.DisposeAsync();
    }
}
