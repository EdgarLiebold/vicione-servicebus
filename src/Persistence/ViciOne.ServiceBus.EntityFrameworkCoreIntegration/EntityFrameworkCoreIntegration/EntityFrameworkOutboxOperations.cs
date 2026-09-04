using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.ProviderAbstractions;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

internal sealed class EntityFrameworkOutboxOperations<TBus, TDbContext> : IEntityFrameworkOutboxOperations<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    internal const int MaximumQuarantinePageSize = 1000;

    readonly string _busKey;
    readonly TDbContext _dbContext;
    readonly IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>> _notification;

    public EntityFrameworkOutboxOperations(TDbContext dbContext,
        IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>> notification,
        BusPersistenceIdentity<TBus> persistenceIdentity)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _notification = notification ?? throw new ArgumentNullException(nameof(notification));
        _busKey = (persistenceIdentity ?? throw new ArgumentNullException(nameof(persistenceIdentity)))
            .Require("Entity Framework outbox operations");
    }

    public async Task<IReadOnlyList<OutboxQuarantineEntry>> GetQuarantinedAsync(int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be greater than zero.");
        if (limit > MaximumQuarantinePageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), limit,
                $"Limit must not exceed {MaximumQuarantinePageSize}.");
        }

        return await _dbContext.Set<OutboxState>()
            .AsNoTracking()
            .Where(x => x.BusKey == _busKey && x.Status == OutboxDeliveryStatus.Quarantined)
            .OrderBy(x => x.Created)
            .ThenBy(x => x.OutboxId)
            .Take(limit)
            .Select(x => new OutboxQuarantineEntry(
                x.OutboxId,
                x.Created,
                x.DeliveryAttempts,
                x.LastFailureKind,
                x.LastFailureTime,
                x.LastFailure,
                x.FailedSequenceNumber,
                x.FailedMessageId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RequeueAsync(Guid outboxId, CancellationToken cancellationToken = default)
    {
        if (outboxId == Guid.Empty)
            throw new ArgumentException("OutboxId must not be empty.", nameof(outboxId));

        var state = await GetOwnedState(outboxId, cancellationToken).ConfigureAwait(false);
        if (state.Status != OutboxDeliveryStatus.Quarantined)
            throw new InvalidOperationException($"Outbox {outboxId} is not quarantined and cannot be requeued.");

        state.Status = OutboxDeliveryStatus.Pending;
        state.NextDeliveryTime = null;
        state.DeliveryAttempts = 0;
        state.LastFailureKind = OutboxFailureKind.None;
        state.LastFailureTime = null;
        state.LastFailure = null;
        state.FailedSequenceNumber = null;
        state.FailedMessageId = null;
        state.Delivered = null;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _notification.Delivered();
    }

    public async Task DiscardAsync(Guid outboxId, CancellationToken cancellationToken = default)
    {
        if (outboxId == Guid.Empty)
            throw new ArgumentException("OutboxId must not be empty.", nameof(outboxId));

        var state = await GetOwnedState(outboxId, cancellationToken).ConfigureAwait(false);
        if (state.Status != OutboxDeliveryStatus.Quarantined)
            throw new InvalidOperationException($"Outbox {outboxId} is not quarantined and cannot be discarded.");

        var messages = await _dbContext.Set<OutboxMessage>()
            .Where(x => x.OutboxId == outboxId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _dbContext.RemoveRange(messages);
        _dbContext.Remove(state);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    async Task<OutboxState> GetOwnedState(Guid outboxId, CancellationToken cancellationToken)
    {
        var state = await _dbContext.Set<OutboxState>()
            .SingleOrDefaultAsync(x => x.OutboxId == outboxId && x.BusKey == _busKey, cancellationToken)
            .ConfigureAwait(false);

        return state ?? throw new KeyNotFoundException($"Outbox {outboxId} was not found for bus '{_busKey}'.");
    }
}
