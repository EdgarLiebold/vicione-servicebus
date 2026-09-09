using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Loads one saga entity by correlation identifier using a configured EF Core query strategy.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal interface ILoadQueryExecutor<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Loads the requested state.</summary>
    /// <param name="dbContext">The DbContext that contains the saga set.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The tracked saga entity, or <see langword="null"/> when no row matches.</returns>
    Task<TSaga?> LoadAsync(DbContext dbContext, Guid correlationId, CancellationToken cancellationToken);
}
