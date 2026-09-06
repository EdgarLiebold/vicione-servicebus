using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Defines transaction, query, and row-lock behavior for an EF Core saga repository.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaRepositoryLockStrategy<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Gets the isolation level used when repository operations create a transaction.</summary>
    IsolationLevel IsolationLevel { get; }

    /// <summary>Applies the configured saga-query transformation.</summary>
    /// <param name="query">The base saga query.</param>
    /// <returns>The transformed query.</returns>
    IQueryable<TSaga> ApplyQueryCustomization(IQueryable<TSaga> query);

    /// <summary>Loads the requested state.</summary>
    /// <param name="context">The DbContext that contains the saga set.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The tracked saga entity, or <see langword="null"/> when no row matches.</returns>
    Task<TSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken);

    /// <summary>Creates the context that will load all saga rows selected by a query.</summary>
    /// <param name="context">The DbContext that contains the saga set.</param>
    /// <param name="query">The saga filter to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A context that loads the selected rows with this strategy's concurrency behavior.</returns>
    Task<SagaLockContext<TSaga>> CreateLockContextAsync(DbContext context, ISagaQuery<TSaga> query, CancellationToken cancellationToken);

    /// <summary>Gets whether repository operations must create a transaction.</summary>
    bool IsTransactionEnabled { get; }
}
