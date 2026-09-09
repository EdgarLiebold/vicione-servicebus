using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Creates a DbContext for the saga repository.</summary>
/// <typeparam name="TSaga">The saga state type that scopes the factory registration.</typeparam>
public interface ISagaDbContextFactory<out TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates a DbContext for a repository operation outside message consumption.</summary>
    /// <returns>The DbContext to use for the operation.</returns>
    DbContext CreateDbContext();

    /// <summary>Obtains a DbContext for a repository operation within message consumption.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The active consumption context.</param>
    /// <returns>The DbContext to use for the operation.</returns>
    DbContext CreateScopedDbContext<T>(ConsumeContext<T> context)
        where T : class;

    /// <summary>Releases or disposes a DbContext obtained from this factory.</summary>
    /// <param name="dbContext">The DbContext returned by <see cref="CreateDbContext"/> or <see cref="CreateScopedDbContext{T}"/>.</param>
    /// <returns>A task that completes when the context has been released.</returns>
    ValueTask ReleaseAsync(DbContext dbContext);
}
