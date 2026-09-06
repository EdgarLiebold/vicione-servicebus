using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Defines the contract for load query executor.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ILoadQueryExecutor<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="dbContext">The db context value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<TSaga?> LoadAsync(DbContext dbContext, Guid correlationId, CancellationToken cancellationToken);
}
