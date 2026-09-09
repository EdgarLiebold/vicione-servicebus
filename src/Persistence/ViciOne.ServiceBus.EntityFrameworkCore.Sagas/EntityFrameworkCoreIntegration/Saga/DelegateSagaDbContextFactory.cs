using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Creates and owns saga DbContext instances through a caller-supplied delegate.</summary>
/// <typeparam name="TSaga">The saga state type that scopes the factory registration.</typeparam>
internal sealed class DelegateSagaDbContextFactory<TSaga> :
    ISagaDbContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly Func<DbContext> _dbContextFactory;

    /// <summary>Initializes a factory that creates DbContext instances through a delegate.</summary>
    /// <param name="dbContextFactory">The delegate invoked for each DbContext request.</param>
    public DelegateSagaDbContextFactory(Func<DbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    /// <summary>Creates a DbContext through the configured delegate.</summary>
    /// <returns>A new DbContext that must later be returned through <see cref="ReleaseAsync"/>.</returns>
    public DbContext CreateDbContext()
    {
        return _dbContextFactory()
            ?? throw new InvalidOperationException("The saga DbContext factory returned null.");
    }

    /// <summary>Creates a DbContext through the configured delegate for a consume operation.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The active consumption context; the delegate does not receive it.</param>
    /// <returns>A new factory-owned DbContext.</returns>
    public DbContext CreateScopedDbContext<T>(ConsumeContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);

        return CreateDbContext();
    }

    /// <summary>Asynchronously disposes a DbContext created by this factory.</summary>
    /// <param name="dbContext">The DbContext to dispose.</param>
    /// <returns>The asynchronous disposal operation for the factory-owned context.</returns>
    public ValueTask ReleaseAsync(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        return dbContext.DisposeAsync();
    }
}
