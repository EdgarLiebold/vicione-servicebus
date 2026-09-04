using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>
/// Provides a container saga db context factory implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class ContainerSagaDbContextFactory<TContext, TSaga> :
    ISagaDbContextFactory<TSaga>
    where TContext : DbContext
    where TSaga : class, ISaga
{
    readonly TContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="dbContext">The db context value.</param>
    public ContainerSagaDbContextFactory(TContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public DbContext Create()
    {
        return _dbContext;
    }

    /// <summary>
    /// Creates scoped.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public DbContext CreateScoped<T>(ConsumeContext<T> context)
        where T : class
    {
        return _dbContext;
    }

    /// <summary>
    /// Performs the release operation.
    /// </summary>
    /// <param name="dbContext">The db context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ValueTask ReleaseAsync(DbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); return default;
    }
}
