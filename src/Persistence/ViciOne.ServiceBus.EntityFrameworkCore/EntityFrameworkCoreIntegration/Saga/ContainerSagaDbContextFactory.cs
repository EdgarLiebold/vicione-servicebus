using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Reuses a dependency-injection-owned DbContext for saga repository operations.</summary>
/// <typeparam name="TContext">The registered DbContext type.</typeparam>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class ContainerSagaDbContextFactory<TContext, TSaga> :
    ISagaDbContextFactory<TSaga>
    where TContext : DbContext
    where TSaga : class, ISaga
{
    readonly TContext _dbContext;

    /// <summary>Initializes a factory that returns a dependency-injection-owned DbContext.</summary>
    /// <param name="dbContext">The scoped DbContext owned by dependency injection.</param>
    public ContainerSagaDbContextFactory(TContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Returns the injected DbContext.</summary>
    /// <returns>The injected DbContext.</returns>
    public DbContext Create()
    {
        return _dbContext;
    }

    /// <summary>Returns the injected DbContext for a consume operation.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The active consumption context; the factory does not use it.</param>
    /// <returns>The injected DbContext.</returns>
    public DbContext CreateScoped<T>(ConsumeContext<T> context)
        where T : class
    {
        return _dbContext;
    }

    /// <summary>Completes without disposing the dependency-injection-owned DbContext.</summary>
    /// <param name="dbContext">The DbContext to leave under container ownership.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask ReleaseAsync(DbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); return default;
    }
}
