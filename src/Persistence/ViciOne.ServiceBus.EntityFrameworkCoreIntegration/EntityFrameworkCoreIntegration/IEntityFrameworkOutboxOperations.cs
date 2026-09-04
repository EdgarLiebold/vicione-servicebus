using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;
/// <summary>
/// Operational API for a single bus/DbContext outbox. Authorization, audit and operator UI belong to the host.
/// </summary>
public interface IEntityFrameworkOutboxOperations<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    Task<IReadOnlyList<OutboxQuarantineEntry>> GetQuarantinedAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task RequeueAsync(Guid outboxId, CancellationToken cancellationToken = default);
    Task DiscardAsync(Guid outboxId, CancellationToken cancellationToken = default);
}


public sealed record OutboxQuarantineEntry(
    Guid OutboxId,
    DateTimeOffset Created,
    int DeliveryAttempts,
    OutboxFailureKind FailureKind,
    DateTimeOffset? FailureTime,
    string? Failure,
    long? FailedSequenceNumber,
    Guid? FailedMessageId);
