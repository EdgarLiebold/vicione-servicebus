using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.ProviderAbstractions;

#nullable enable

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;
/// <summary>
/// EF Core persistent durable-send store with atomic retained-storage admission and fenced delivery ownership.
/// </summary>
/// <remarks>
/// Quarantined records remain capacity-owned until an operator explicitly requeues or discards them. Successful delivery
/// removes the record in the same transaction that releases capacity. Claims use compare-and-set updates, so competing
/// agents cannot own the same record even when the provider's normal read isolation is snapshot based.
/// </remarks>
internal sealed class EntityFrameworkDurableSendStore<TBus, TDbContext> : IDurableSendStore<TBus>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    readonly IDbContextFactory<TDbContext> _dbContextFactory;
    readonly IEntityFrameworkDurableSendCommitDurabilityValidator<TBus> _commitDurabilityValidator;
    readonly string _storeKey;
    readonly SemaphoreSlim _initializationGate = new(1, 1);
    volatile bool _initialized;

    public EntityFrameworkDurableSendStore(
        IDbContextFactory<TDbContext> dbContextFactory,
        BusPersistenceIdentity<TBus> persistenceIdentity,
        IEntityFrameworkDurableSendCommitDurabilityValidator<TBus> commitDurabilityValidator)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        ArgumentNullException.ThrowIfNull(persistenceIdentity);
        _commitDurabilityValidator = commitDurabilityValidator
            ?? throw new ArgumentNullException(nameof(commitDurabilityValidator));
        _storeKey = persistenceIdentity.Require("Entity Framework Durable Sender");
    }

    public async Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        DurableSendStoreLimits limits,
        DateTimeOffset enqueuedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Validate();
        if (message.StorageSize > limits.MaximumStoredBytes)
            throw new DurableSendCapacityExceededException(
                $"Durable send size {message.StorageSize} exceeds the store byte limit {limits.MaximumStoredBytes}.",
                0,
                0);

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);

        var existing = await db.Set<DurableSendRecord>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.StoreKey == _storeKey && x.Id == message.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            EnsureSameIntent(existing, message);
            var storage = await ReadCapacityAsync(db, cancellationToken).ConfigureAwait(false);
            var disposition = existing.Status == DurableSendStatus.Quarantined
                ? DurableSendAdmissionDisposition.AlreadyQuarantined
                : DurableSendAdmissionDisposition.AlreadyAccepted;
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new DurableSendAdmissionResult(message.Id, disposition, storage.StoredCount, storage.StoredBytes);
        }

        await IncrementCapacityAsync(db, message.StorageSize, limits, cancellationToken).ConfigureAwait(false);
        db.Set<DurableSendRecord>().Add(ToRecord(message, enqueuedAt));

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var storage = await ReadCapacityAsync(db, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new DurableSendAdmissionResult(
                message.Id,
                DurableSendAdmissionDisposition.Accepted,
                storage.StoredCount,
                storage.StoredBytes);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

            // A racing idempotent admission is success only when the exact immutable intent is now committed. Any
            // different message under the same id is a contract violation, and unrelated provider errors remain visible.
            await using var verification = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var raced = await verification.Set<DurableSendRecord>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.StoreKey == _storeKey && x.Id == message.Id.Value, cancellationToken)
                .ConfigureAwait(false);
            if (raced is null)
                throw;

            EnsureSameIntent(raced, message);
            var storage = await ReadCapacityAsync(verification, cancellationToken).ConfigureAwait(false);
            var disposition = raced.Status == DurableSendStatus.Quarantined
                ? DurableSendAdmissionDisposition.AlreadyQuarantined
                : DurableSendAdmissionDisposition.AlreadyAccepted;
            return new DurableSendAdmissionResult(message.Id, disposition, storage.StoredCount, storage.StoredBytes);
        }
    }

    public async Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(
        DateTimeOffset now,
        int maximumCount,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        DurableSendOperationLimits.ValidateClaimCount(maximumCount, nameof(maximumCount));
        if (leaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var utcNow = now.UtcDateTime;
        var candidateIds = await db.Set<DurableSendRecord>().AsNoTracking()
            .Where(x => x.StoreKey == _storeKey
                && (x.Status == DurableSendStatus.Pending
                    || x.Status == DurableSendStatus.RetryScheduled
                    || x.Status == DurableSendStatus.AwaitingConsumerCompletion)
                && (x.Status == DurableSendStatus.Pending || x.NextAttemptAt <= utcNow)
                && (x.LeaseToken == null || x.LeaseExpiresAt <= utcNow))
            .OrderBy(x => x.EnqueuedAt)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(maximumCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidateIds.Count == 0)
            return Array.Empty<DurableSendDelivery>();

        var leaseToken = Guid.NewGuid();
        var leaseExpires = (now + leaseDuration).UtcDateTime;
        var claimedCount = 0;

        foreach (var id in candidateIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var updated = await db.Set<DurableSendRecord>()
                .Where(x => x.StoreKey == _storeKey
                    && x.Id == id
                    && (x.Status == DurableSendStatus.Pending
                        || x.Status == DurableSendStatus.RetryScheduled
                        || x.Status == DurableSendStatus.AwaitingConsumerCompletion)
                    && (x.Status == DurableSendStatus.Pending || x.NextAttemptAt <= utcNow)
                    && (x.LeaseToken == null || x.LeaseExpiresAt <= utcNow))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.LeaseToken, leaseToken)
                    .SetProperty(x => x.LeaseExpiresAt, leaseExpires), cancellationToken)
                .ConfigureAwait(false);
            claimedCount += updated;
        }

        if (claimedCount == 0)
            return Array.Empty<DurableSendDelivery>();

        var rows = await db.Set<DurableSendRecord>().AsNoTracking()
            .Where(x => x.StoreKey == _storeKey && x.LeaseToken == leaseToken)
            .OrderBy(x => x.EnqueuedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var lease = new DurableSendLease(leaseToken, new DateTimeOffset(DateTime.SpecifyKind(leaseExpires, DateTimeKind.Utc)));
        return rows.Select(row => ToDelivery(row, lease)).ToArray();
    }

    public async Task<bool> MarkDeliveredAsync(
        DurableSendId id,
        DurableSendLease lease,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken = default)
    {
        _ = deliveredAt;
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);

        DurableSendRecord? row = await TryGetOwnedAsync(db, id, lease, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            bool exists = await RecordExistsAsync(db, id, cancellationToken).ConfigureAwait(false);
            if (exists)
                throw new InvalidOperationException($"Durable send '{id}' is not owned by lease '{lease.Token}'.");

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        await DecrementCapacityAsync(db, row.StorageSize, cancellationToken).ConfigureAwait(false);
        db.Set<DurableSendRecord>().Remove(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> AwaitConsumerCompletionAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        int updated = await db.Set<DurableSendRecord>()
            .Where(x => x.StoreKey == _storeKey && x.Id == id.Value && x.LeaseToken == lease.Token)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, DurableSendStatus.AwaitingConsumerCompletion)
                .SetProperty(x => x.DeliveryAttempts, deliveryAttempts)
                .SetProperty(x => x.NextAttemptAt, nextAttemptAt.UtcDateTime)
                .SetProperty(x => x.LastFailureKind, DurableSendFailureKind.None)
                .SetProperty(x => x.LastFailureType, (string?)null)
                .SetProperty(x => x.LastFailureAt, (DateTime?)null)
                .SetProperty(x => x.LeaseToken, (Guid?)null)
                .SetProperty(x => x.LeaseExpiresAt, (DateTime?)null), cancellationToken)
            .ConfigureAwait(false);
        if (updated == 1)
            return true;

        bool stillExists = await db.Set<DurableSendRecord>().AsNoTracking()
            .AnyAsync(x => x.StoreKey == _storeKey && x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (!stillExists)
            return false; // Consumer completion won the dispatch/transition race.

        throw new InvalidOperationException($"Durable send '{id}' is not owned by lease '{lease.Token}'.");
    }

    public async Task<bool> CompleteConsumerDeliveryAsync(
        DurableSendId id,
        Guid generationToken,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        _ = completedAt;
        if (generationToken == Guid.Empty)
            throw new ArgumentException("Generation token must not be empty.", nameof(generationToken));
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);

        DurableSendRecord? row = await db.Set<DurableSendRecord>().SingleOrDefaultAsync(
            x => x.StoreKey == _storeKey && x.Id == id.Value && x.GenerationToken == generationToken,
            cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        // Logical consumer success resolves overlapping send-side uncertainty for the same persisted incarnation.
        // Generation fencing prevents a stale capability from retiring a later re-admission under the same durable id.
        await DecrementCapacityAsync(db, row.StorageSize, cancellationToken).ConfigureAwait(false);
        db.Set<DurableSendRecord>().Remove(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public Task<bool> ScheduleRetryAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default)
        => MutateOwnedAsync(id, lease, (row) =>
        {
            row.Status = DurableSendStatus.RetryScheduled;
            row.DeliveryAttempts = deliveryAttempts;
            row.NextAttemptAt = nextAttemptAt.UtcDateTime;
            row.LastFailureKind = failureKind;
            row.LastFailureType = BoundFailureType(failureType);
            row.LastFailureAt = failedAt.UtcDateTime;
            row.LeaseToken = null;
            row.LeaseExpiresAt = null;
        }, cancellationToken);

    public Task<bool> QuarantineAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset quarantinedAt,
        CancellationToken cancellationToken = default)
        => MutateOwnedAsync(id, lease, (row) =>
        {
            // Quarantine remains capacity-owned. This is intentional: repeated permanent failures cannot turn durable
            // storage into an unbounded database. The host must explicitly requeue or discard terminal records.
            row.Status = DurableSendStatus.Quarantined;
            row.DeliveryAttempts = deliveryAttempts;
            row.NextAttemptAt = null;
            row.LastFailureKind = failureKind;
            row.LastFailureType = BoundFailureType(failureType);
            row.LastFailureAt = quarantinedAt.UtcDateTime;
            row.QuarantinedAt = quarantinedAt.UtcDateTime;
            row.LeaseToken = null;
            row.LeaseExpiresAt = null;
        }, cancellationToken);

    public async Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        DurableSendCapacityState storage = await ReadCapacityAsync(db, cancellationToken).ConfigureAwait(false);

        // One aggregate query owns all record-state observations. Metrics/health must not turn into a fan-out of
        // independent database round-trips on every snapshot cadence. The capacity row remains a separate query because
        // it is the authoritative hard-bound ledger, not a derived record count.
        var aggregate = await db.Set<DurableSendRecord>().AsNoTracking()
            .Where(x => x.StoreKey == _storeKey)
            .GroupBy(static _ => 1)
            .Select(group => new
            {
                PendingCount = group.Count(x => x.Status == DurableSendStatus.Pending
                    || x.Status == DurableSendStatus.RetryScheduled
                    || x.Status == DurableSendStatus.AwaitingConsumerCompletion),
                RetryCount = group.Count(x => x.Status == DurableSendStatus.RetryScheduled),
                AwaitingConsumerCount = group.Count(x => x.Status == DurableSendStatus.AwaitingConsumerCompletion),
                QuarantinedCount = group.Count(x => x.Status == DurableSendStatus.Quarantined),
                OldestPending = group
                    .Where(x => x.Status == DurableSendStatus.Pending
                        || x.Status == DurableSendStatus.RetryScheduled
                        || x.Status == DurableSendStatus.AwaitingConsumerCompletion)
                    .Select(x => (DateTime?)x.EnqueuedAt)
                    .Min(),
            })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new DurableSendStoreSnapshot(
            storage.StoredCount,
            storage.StoredBytes,
            aggregate?.PendingCount ?? 0,
            aggregate?.RetryCount ?? 0,
            aggregate?.AwaitingConsumerCount ?? 0,
            aggregate?.QuarantinedCount ?? 0,
            aggregate?.OldestPending is { } oldest ? ToUtcOffset(oldest) : null);
    }

    public async Task<DurableSendQuarantinePage> GetQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default)
    {
        DurableSendQuarantineSeek seek = DurableSendQuarantinePagination.Validate(query);

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<DurableSendRecord> rowsQuery = db.Set<DurableSendRecord>().AsNoTracking()
            .Where(x => x.StoreKey == _storeKey && x.Status == DurableSendStatus.Quarantined);
        if (seek.HasValue)
        {
            DateTime quarantinedAt = seek.QuarantinedAt.UtcDateTime;
            Guid id = seek.Id.Value;
            rowsQuery = rowsQuery.Where(x =>
                x.QuarantinedAt < quarantinedAt
                || x.QuarantinedAt == quarantinedAt && x.Id.CompareTo(id) > 0);
        }

        var rows = await rowsQuery
            .OrderByDescending(x => x.QuarantinedAt)
            .ThenBy(x => x.Id)
            .Take(checked(query.PageSize + 1))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return DurableSendQuarantinePagination.CreatePage(rows.Select(ToQuarantineEntry), query.PageSize);
    }

    public async Task<DurableSendOperationResult> RequeueAsync(
        DurableSendId id,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var updated = await db.Set<DurableSendRecord>()
            .Where(x => x.StoreKey == _storeKey && x.Id == id.Value && x.Status == DurableSendStatus.Quarantined)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, DurableSendStatus.RetryScheduled)
                .SetProperty(x => x.DeliveryAttempts, 0)
                .SetProperty(x => x.NextAttemptAt, dueAt.UtcDateTime)
                .SetProperty(x => x.QuarantinedAt, (DateTime?)null)
                .SetProperty(x => x.LastFailureKind, DurableSendFailureKind.None)
                .SetProperty(x => x.LastFailureType, (string?)null)
                .SetProperty(x => x.LastFailureAt, (DateTime?)null)
                .SetProperty(x => x.LeaseToken, (Guid?)null)
                .SetProperty(x => x.LeaseExpiresAt, (DateTime?)null), cancellationToken)
            .ConfigureAwait(false);

        // Quarantine already consumed capacity, therefore no capacity change is necessary or allowed here.
        if (updated == 1)
            return new DurableSendOperationResult(id, DurableSendOperationOutcome.Requeued);

        DurableSendStatus? status = await db.Set<DurableSendRecord>().AsNoTracking()
            .Where(x => x.StoreKey == _storeKey && x.Id == id.Value)
            .Select(x => (DurableSendStatus?)x.Status)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return new DurableSendOperationResult(
            id,
            status.HasValue ? DurableSendOperationOutcome.NotQuarantined : DurableSendOperationOutcome.NotFound);
    }

    public async Task<DurableSendOperationResult> DiscardQuarantinedAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var row = await db.Set<DurableSendRecord>().SingleOrDefaultAsync(
            x => x.StoreKey == _storeKey && x.Id == id.Value,
            cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new DurableSendOperationResult(id, DurableSendOperationOutcome.NotFound);
        }
        if (row.Status != DurableSendStatus.Quarantined)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new DurableSendOperationResult(id, DurableSendOperationOutcome.NotQuarantined);
        }

        await DecrementCapacityAsync(db, row.StorageSize, cancellationToken).ConfigureAwait(false);
        db.Set<DurableSendRecord>().Remove(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new DurableSendOperationResult(id, DurableSendOperationOutcome.Discarded);
    }

    async Task<bool> MutateOwnedAsync(
        DurableSendId id,
        DurableSendLease lease,
        Action<DurableSendRecord> mutate,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        DurableSendRecord? row = await TryGetOwnedAsync(db, id, lease, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            bool exists = await RecordExistsAsync(db, id, cancellationToken).ConfigureAwait(false);
            if (exists)
                throw new InvalidOperationException($"Durable send '{id}' is not owned by lease '{lease.Token}'.");

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        mutate(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    async Task<DurableSendRecord?> TryGetOwnedAsync(
        TDbContext db,
        DurableSendId id,
        DurableSendLease lease,
        CancellationToken cancellationToken)
        => await db.Set<DurableSendRecord>().SingleOrDefaultAsync(
            x => x.StoreKey == _storeKey && x.Id == id.Value && x.LeaseToken == lease.Token,
            cancellationToken).ConfigureAwait(false);

    async Task<bool> RecordExistsAsync(
        TDbContext db,
        DurableSendId id,
        CancellationToken cancellationToken)
        => await db.Set<DurableSendRecord>().AsNoTracking().AnyAsync(
            x => x.StoreKey == _storeKey && x.Id == id.Value,
            cancellationToken).ConfigureAwait(false);

    async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await _initializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
                return;

            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await _commitDurabilityValidator.ValidateAsync(db, cancellationToken).ConfigureAwait(false);

            if (await db.Set<DurableSendCapacityState>().AsNoTracking()
                    .AnyAsync(x => x.StoreKey == _storeKey, cancellationToken).ConfigureAwait(false))
            {
                _initialized = true;
                return;
            }

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
            var existing = await db.Set<DurableSendCapacityState>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.StoreKey == _storeKey, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                // Reconstruct the authoritative capacity ledger with one server-side aggregate. Initialization must not
                // materialize every retained row into process memory merely to count/sum it; a missing ledger can occur
                // exactly during recovery/migration, when the retained set may be at its configured bound.
                var retained = await db.Set<DurableSendRecord>().AsNoTracking()
                    .Where(x => x.StoreKey == _storeKey)
                    .GroupBy(static _ => 1)
                    .Select(group => new
                    {
                        Count = group.Count(),
                        Bytes = group.Sum(x => x.StorageSize),
                    })
                    .SingleOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
                db.Set<DurableSendCapacityState>().Add(new DurableSendCapacityState
                {
                    StoreKey = _storeKey,
                    StoredCount = retained?.Count ?? 0,
                    StoredBytes = retained?.Bytes ?? 0,
                });

                try
                {
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateException)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    db.ChangeTracker.Clear();
                    if (!await db.Set<DurableSendCapacityState>().AsNoTracking()
                            .AnyAsync(x => x.StoreKey == _storeKey, cancellationToken).ConfigureAwait(false))
                        throw;
                }
            }
            else
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            _initialized = true;
        }
        finally
        {
            _initializationGate.Release();
        }
    }

    async Task<DurableSendCapacityState> ReadCapacityAsync(TDbContext db, CancellationToken cancellationToken)
        => await db.Set<DurableSendCapacityState>().AsNoTracking()
            .SingleAsync(x => x.StoreKey == _storeKey, cancellationToken)
            .ConfigureAwait(false);

    async Task IncrementCapacityAsync(
        TDbContext db,
        long bytes,
        DurableSendStoreLimits limits,
        CancellationToken cancellationToken)
    {
        if (bytes > limits.MaximumStoredBytes)
            throw new DurableSendCapacityExceededException(
                $"Durable send size {bytes} exceeds the store byte limit {limits.MaximumStoredBytes}.",
                0,
                0);

        var updated = await db.Set<DurableSendCapacityState>()
            .Where(x => x.StoreKey == _storeKey
                && x.StoredCount < limits.MaximumStoredCount
                && x.StoredBytes <= limits.MaximumStoredBytes - bytes)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.StoredCount, x => x.StoredCount + 1)
                .SetProperty(x => x.StoredBytes, x => x.StoredBytes + bytes), cancellationToken)
            .ConfigureAwait(false);
        if (updated == 1)
            return;

        var current = await ReadCapacityAsync(db, cancellationToken).ConfigureAwait(false);
        throw new DurableSendCapacityExceededException(
            $"Durable sender storage capacity would be exceeded ({current.StoredCount}/{limits.MaximumStoredCount} records, "
            + $"{current.StoredBytes}/{limits.MaximumStoredBytes} bytes currently owned).",
            current.StoredCount,
            current.StoredBytes);
    }

    async Task DecrementCapacityAsync(TDbContext db, long bytes, CancellationToken cancellationToken)
    {
        var updated = await db.Set<DurableSendCapacityState>()
            .Where(x => x.StoreKey == _storeKey && x.StoredCount > 0 && x.StoredBytes >= bytes)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.StoredCount, x => x.StoredCount - 1)
                .SetProperty(x => x.StoredBytes, x => x.StoredBytes - bytes), cancellationToken)
            .ConfigureAwait(false);
        if (updated != 1)
            throw new InvalidOperationException("Durable sender capacity state is inconsistent with the retained record.");
    }

    DurableSendRecord ToRecord(SerializedDurableSend message, DateTimeOffset enqueuedAt)
        => new()
        {
            StoreKey = _storeKey,
            Id = message.Id.Value,
            GenerationToken = Guid.NewGuid(),
            ContractIdentity = message.ContractIdentity.ToString(),
            DestinationAddress = message.DestinationAddress.AbsoluteUri,
            ContentType = message.ContentType,
            Body = message.Body.ToArray(),
            Metadata = message.Metadata.IsEmpty ? null : message.Metadata.ToArray(),
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            StorageSize = message.StorageSize,
            Status = DurableSendStatus.Pending,
            EnqueuedAt = enqueuedAt.UtcDateTime,
        };

    static DurableSendDelivery ToDelivery(DurableSendRecord row, DurableSendLease lease)
        => new()
        {
            Message = new SerializedDurableSend
            {
                Id = new DurableSendId(row.Id),
                ContractIdentity = MessageContractIdentity.Parse(row.ContractIdentity),
                DestinationAddress = new Uri(row.DestinationAddress, UriKind.Absolute),
                ContentType = row.ContentType,
                Body = row.Body,
                Metadata = row.Metadata ?? Array.Empty<byte>(),
                MessageId = row.MessageId,
                CorrelationId = row.CorrelationId,
            },
            GenerationToken = row.GenerationToken,
            EnqueuedAt = ToUtcOffset(row.EnqueuedAt),
            DeliveryAttempts = row.DeliveryAttempts,
            Status = row.Status,
            Lease = lease,
        };

    static DurableSendQuarantineEntry ToQuarantineEntry(DurableSendRecord row)
        => new()
        {
            Id = new DurableSendId(row.Id),
            ContractIdentity = MessageContractIdentity.Parse(row.ContractIdentity),
            DestinationAddress = new Uri(row.DestinationAddress, UriKind.Absolute),
            EnqueuedAt = ToUtcOffset(row.EnqueuedAt),
            QuarantinedAt = ToUtcOffset(row.QuarantinedAt!.Value),
            DeliveryAttempts = row.DeliveryAttempts,
            FailureKind = row.LastFailureKind,
            FailureType = row.LastFailureType,
        };

    static DateTimeOffset ToUtcOffset(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    static void EnsureSameIntent(DurableSendRecord existing, SerializedDurableSend message)
    {
        if (string.Equals(existing.ContractIdentity, message.ContractIdentity.ToString(), StringComparison.Ordinal)
            && string.Equals(existing.DestinationAddress, message.DestinationAddress.AbsoluteUri, StringComparison.Ordinal)
            && string.Equals(existing.ContentType, message.ContentType, StringComparison.Ordinal)
            && existing.MessageId == message.MessageId
            && existing.CorrelationId == message.CorrelationId
            && existing.Body.AsSpan().SequenceEqual(message.Body.Span)
            && (existing.Metadata ?? Array.Empty<byte>()).AsSpan().SequenceEqual(message.Metadata.Span))
            return;

        throw new DurableSendIdentityConflictException(message.Id);
    }

    static string? BoundFailureType(string? failureType)
        => failureType is { Length: > 512 } ? failureType[..512] : failureType;
}
