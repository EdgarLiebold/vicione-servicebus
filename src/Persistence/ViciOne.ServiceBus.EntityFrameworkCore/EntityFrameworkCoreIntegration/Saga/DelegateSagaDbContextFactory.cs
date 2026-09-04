using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>
/// Provides a delegate saga db context factory implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class DelegateSagaDbContextFactory<TSaga> :
    ISagaDbContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly Func<DbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="dbContextFactory">The db context factory value.</param>
    public DelegateSagaDbContextFactory(Func<DbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public DbContext Create()
    {
        return _dbContextFactory();
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
        return _dbContextFactory();
    }

    /// <summary>
    /// Performs the release operation.
    /// </summary>
    /// <param name="dbContext">The db context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ValueTask ReleaseAsync(DbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); return dbContext.DisposeAsync();
    }
}
