using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;
/// <summary>Operational API for a single bus/DbContext outbox. Authorization, audit and operator UI belong to the host.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
/// <typeparam name="TDbContext">The db context type.</typeparam>
public interface IEntityFrameworkOutboxOperations<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    /// <summary>Loads the oldest quarantined outbox rows owned by this bus.</summary>
    /// <param name="limit">The maximum number of rows to return, from 1 through 1000.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The quarantined rows ordered by creation time and outbox identifier.</returns>
    Task<IReadOnlyList<OutboxQuarantineEntry>> GetQuarantinedAsync(int limit = 100, CancellationToken cancellationToken = default);
    /// <summary>Returns a quarantined outbox row to pending delivery and clears its failure state.</summary>
    /// <param name="outboxId">The outbox identifier owned by this bus.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RequeueAsync(Guid outboxId, CancellationToken cancellationToken = default);
    /// <summary>Deletes a quarantined outbox row and all of its persisted messages.</summary>
    /// <param name="outboxId">The outbox identifier owned by this bus.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DiscardAsync(Guid outboxId, CancellationToken cancellationToken = default);
}
