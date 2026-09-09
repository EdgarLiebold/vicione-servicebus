using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

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

        IQueryable<OutboxState> query = _dbContext.Set<OutboxState>()
            .AsNoTracking()
            .Where(x => x.BusKey == _busKey && x.Status == OutboxDeliveryStatus.Quarantined);

        // SQLite persists the UTC-normalized value as canonical text and rejects ordering by a
        // DateTimeOffset expression. Ordering by that representation keeps the query bounded in
        // the database instead of materializing every quarantined entry for client-side sorting.
        IOrderedQueryable<OutboxState> orderedQuery = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
            ? query.OrderBy(x => x.Created.ToString()).ThenBy(x => x.OutboxId)
            : query.OrderBy(x => x.Created).ThenBy(x => x.OutboxId);

        return await orderedQuery
            .Take(limit)
            .Select(x => new OutboxQuarantineEntry(
                x.OutboxId,
                x.Created,
                x.DeliveryAttempts,
                x.LastFailureKind,
                x.LastFailureCode,
                x.LastFailureTime,
                x.LastExceptionType,
                x.FailedSequenceNumber,
                x.FailedMessageId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RequeueAsync(Guid outboxId, CancellationToken cancellationToken = default)
    {
        if (outboxId == Guid.Empty)
            throw new ArgumentException("OutboxId must not be empty.", nameof(outboxId));

        int updated = await _dbContext.Set<OutboxState>()
            .Where(state => state.OutboxId == outboxId
                && state.BusKey == _busKey
                && state.Status == OutboxDeliveryStatus.Quarantined)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(state => state.Status, OutboxDeliveryStatus.Pending)
                .SetProperty(state => state.NextDeliveryTime, (DateTimeOffset?)null)
                .SetProperty(state => state.DeliveryAttempts, 0)
                .SetProperty(state => state.LastFailureKind, OutboxFailureKind.None)
                .SetProperty(state => state.LastFailureCode, OutboxFailureCode.None)
                .SetProperty(state => state.LastFailureTime, (DateTimeOffset?)null)
                .SetProperty(state => state.LastExceptionType, (string?)null)
                .SetProperty(state => state.FailedSequenceNumber, (long?)null)
                .SetProperty(state => state.FailedMessageId, (Guid?)null)
                .SetProperty(state => state.Delivered, (DateTimeOffset?)null),
                cancellationToken)
            .ConfigureAwait(false);

        if (updated == 0)
            await ThrowForUnavailableMutationAsync(outboxId, "requeued", cancellationToken).ConfigureAwait(false);

        _notification.SignalDelivery();
    }

    public async Task DiscardAsync(Guid outboxId, CancellationToken cancellationToken = default)
    {
        if (outboxId == Guid.Empty)
            throw new ArgumentException("OutboxId must not be empty.", nameof(outboxId));

        int deleted;
        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
        {
            int claimed = await _dbContext.Set<OutboxState>()
                .Where(state => state.OutboxId == outboxId
                    && state.BusKey == _busKey
                    && state.Status == OutboxDeliveryStatus.Quarantined)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(state => state.Status, OutboxDeliveryStatus.Discarding),
                    cancellationToken)
                .ConfigureAwait(false);

            if (claimed == 0)
            {
                deleted = 0;
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                await _dbContext.Set<OutboxMessage>()
                    .Where(message => message.OutboxId == outboxId)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
                deleted = await _dbContext.Set<OutboxState>()
                    .Where(state => state.OutboxId == outboxId
                        && state.BusKey == _busKey
                        && state.Status == OutboxDeliveryStatus.Discarding)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (deleted != 1)
                    throw new InvalidOperationException($"Discarding outbox {outboxId} lost its operator-owned state row.");

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        if (deleted == 0)
            await ThrowForUnavailableMutationAsync(outboxId, "discarded", cancellationToken).ConfigureAwait(false);
    }

    async Task ThrowForUnavailableMutationAsync(Guid outboxId, string operation, CancellationToken cancellationToken)
    {
        OutboxDeliveryStatus? status = await _dbContext.Set<OutboxState>()
            .AsNoTracking()
            .Where(state => state.OutboxId == outboxId && state.BusKey == _busKey)
            .Select(state => (OutboxDeliveryStatus?)state.Status)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (status is null)
            throw new KeyNotFoundException($"Outbox {outboxId} was not found for bus '{_busKey}'.");

        throw new InvalidOperationException($"Outbox {outboxId} has status '{status}' and cannot be {operation}.");
    }
}
