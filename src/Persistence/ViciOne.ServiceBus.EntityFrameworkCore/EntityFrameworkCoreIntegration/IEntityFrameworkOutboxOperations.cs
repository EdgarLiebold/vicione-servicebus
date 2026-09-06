using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;
/// <summary>
/// Operational API for a single bus/DbContext outbox. Authorization, audit and operator UI belong to the host.
/// </summary>
public interface IEntityFrameworkOutboxOperations<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    /// <summary>
    /// Gets quarantined.
    /// </summary>
    /// <param name="limit">The limit value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<IReadOnlyList<OutboxQuarantineEntry>> GetQuarantinedAsync(int limit = 100, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the requeue operation.
    /// </summary>
    /// <param name="outboxId">The outbox id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task RequeueAsync(Guid outboxId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the discard operation.
    /// </summary>
    /// <param name="outboxId">The outbox id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DiscardAsync(Guid outboxId, CancellationToken cancellationToken = default);
}
