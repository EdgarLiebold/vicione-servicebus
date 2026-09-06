using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Defines the contract for saga repository lock strategy.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ISagaRepositoryLockStrategy<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Gets the isolation level value.
    /// </summary>
    IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Performs the apply query customization operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <returns>The result of the operation.</returns>
    IQueryable<TSaga> ApplyQueryCustomization(IQueryable<TSaga> query);

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<TSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates lock context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<SagaLockContext<TSaga>> CreateLockContextAsync(DbContext context, ISagaQuery<TSaga> query, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the is transaction enabled value.
    /// </summary>
    bool IsTransactionEnabled { get; }
}
