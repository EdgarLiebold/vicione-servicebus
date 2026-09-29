using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal class EntityFrameworkScopedBusContext<TBus, TDbContext> :
    ScopedBusContext,
    OutboxSendContext,
    IEntityFrameworkTransactionalOutbox<TBus, TDbContext>,
    IDisposable
    where TBus : class, IBus
    where TDbContext : DbContext
{
    readonly TBus _bus;
    readonly IClientFactory _clientFactory;
    readonly TDbContext _dbContext;
    readonly IMessageContractCatalog _contractCatalog;
    readonly string _persistenceIdentity;
    readonly IServiceProvider _provider;
    readonly TimeProvider _timeProvider;
    readonly ReliableMessagingOptions<TBus> _options;
    readonly EntityFrameworkOutboxWriteCoordinator _writeCoordinator = new();
    readonly Dictionary<Guid, StagedSend> _staged = [];
    CapacityBaseline? _capacityBaseline;
    bool _saveIncludedStaged;
    bool _disposed;
    IPublishEndpoint? _publishEndpoint;
    IScopedClientFactory? _scopedClientFactory;
    ISendEndpointProvider? _sendEndpointProvider;

    internal bool HasActiveSession => _staged.Count > 0;

    public EntityFrameworkScopedBusContext(TBus bus, TDbContext dbContext,
        IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>> notification,
        IClientFactory clientFactory, IServiceProvider provider, TimeProvider timeProvider,
        BusPersistenceIdentity<TBus> persistenceIdentity)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        ArgumentNullException.ThrowIfNull(notification);
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _contractCatalog = provider.GetRequiredService<IMessageContractCatalog>();
        _options = provider.GetRequiredService<IOptions<ReliableMessagingOptions<TBus>>>().Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _persistenceIdentity = (persistenceIdentity ?? throw new ArgumentNullException(nameof(persistenceIdentity)))
            .Require("Entity Framework transactional outbox");
        _dbContext.SavingChanges += OnSavingChanges;
        _dbContext.SavedChanges += OnSavedChanges;
        _dbContext.SaveChangesFailed += OnSaveChangesFailed;
    }

    public ISendEndpointProvider SendEndpointProvider => _sendEndpointProvider ??= new OutboxSendEndpointProvider(this, GetSendEndpointProvider());

    public IPublishEndpoint PublishEndpoint =>
        _publishEndpoint ??= new PublishEndpoint(new OutboxPublishEndpointProvider(this, GetPublishEndpointProvider()));

    public IScopedClientFactory ClientFactory => _scopedClientFactory ??= GetClientFactory();

    public object? GetService(Type serviceType) => _provider.GetService(serviceType);

    public Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ThrowIfDisposed();

        return _writeCoordinator.ExecuteAsync(async () =>
        {
            ThrowIfDisposed();
            if (_staged.Count > 0)
                EnsureStagedRecordsPending();

            if (!context.MessageId.HasValue)
                throw new MessageException(typeof(T), "The SendContext MessageId must be present");
            Uri destination = context.DestinationAddress
                ?? throw new MessageException(typeof(T), "The SendContext DestinationAddress must be present");
            DateTimeOffset now = _timeProvider.GetUtcNow();
            PayloadAdmissionRuntime<TBus>? admissionRuntime = _provider.GetService<PayloadAdmissionRuntime<TBus>>();
            if (admissionRuntime is null)
            {
                throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                        "Entity Framework transactional outbox",
                        typeof(TBus).ToString(),
                        "The payload-admission runtime is missing.",
                        "Register payload admission for this bus before using the transactional outbox"));
            }

            byte[] body = PayloadAdmissionTransportBoundary.Admit(admissionRuntime, context).ToArray();
            string contentType = context.ContentType?.ToString() ?? context.Serialization.DefaultContentType.ToString();
            if (!context.TryGetPayload(out PayloadAdmissionSerializationContext? admission)
                || !admission.TryCreateDurableProof(contentType, out DurablePayloadAdmissionProof proof)
                || !proof.MatchesEnvelope(body, contentType))
                throw new InvalidOperationException("The transactional outbox has no complete payload admission proof for its serialized envelope.");

            byte[] metadata = ReliableEnvelopeMetadataCodec.Capture(context, now, proof).ToArray();
            Guid id = context.MessageId.Value;
            if (_staged.ContainsKey(id)
                || _dbContext.ChangeTracker.Entries<DurableSendRecord>()
                    .Any(entry => entry.Entity.Id == id && entry.Entity.StoreKey == _persistenceIdentity))
                throw new InvalidOperationException($"The transactional outbox message '{id}' is already staged in this session.");
            var record = new DurableSendRecord
            {
                StoreKey = _persistenceIdentity,
                Id = id,
                GenerationToken = Guid.NewGuid(),
                ContractIdentity = _contractCatalog.GetIdentity(typeof(T)).ToString(),
                DestinationAddress = destination.AbsoluteUri,
                ContentType = contentType,
                Body = body,
                Metadata = metadata,
                MessageId = context.MessageId,
                CorrelationId = context.CorrelationId,
                StorageSize = checked(body.LongLength + metadata.LongLength),
                Status = DurableSendStatus.Pending,
                EnqueuedAt = now.UtcDateTime,
                DueAt = context.Delay.HasValue ? (now + context.Delay.Value).UtcDateTime : null,
                NextAttemptAt = context.Delay.HasValue ? (now + context.Delay.Value).UtcDateTime : null,
            };

            CapacityReservation reservation = await ReserveCapacityAsync(record.StorageSize, cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                EntityEntry<DurableSendRecord> entry = _dbContext.Add(record);
                _staged.Add(id, new StagedSend(entry, record.StorageSize, record.GenerationToken));
            }
            catch
            {
                reservation.Capacity.StoredCount = reservation.PreviousCount;
                reservation.Capacity.StoredBytes = reservation.PreviousBytes;
                if (_staged.Count == 0)
                    _capacityBaseline = null;
                throw;
            }
        }, cancellationToken);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _writeCoordinator.ExecuteAsync(async () =>
        {
            ThrowIfDisposed();
            EnsureStagedRecordsPending();

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (_staged.Count == 0)
                return;

            throw new InvalidOperationException("The transactional outbox SaveChanges operation did not persist the staged outbox state.");
        }, cancellationToken);
    }

    public Task AbortAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _writeCoordinator.ExecuteAsync(() =>
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            DetachPendingOutbox();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Exception? uncommitted = null;
        _writeCoordinator.ExecuteSynchronous(() =>
        {
            if (_staged.Count > 0)
            {
                Guid[] abandonedIds = _staged.Keys.ToArray();
                try
                {
                    DetachPendingOutbox();
                }
                catch (ObjectDisposedException)
                {
                    _staged.Clear();
                }

                uncommitted = new InvalidOperationException(
                    $"The transactional outbox for {TypeCache<TBus>.ShortName}/{TypeCache<TDbContext>.ShortName} was disposed without commit. "
                    + $"{abandonedIds.Length} staged outbox records were discarded to prevent accidental later persistence.");
            }

            _dbContext.SavingChanges -= OnSavingChanges;
            _dbContext.SavedChanges -= OnSavedChanges;
            _dbContext.SaveChangesFailed -= OnSaveChangesFailed;
            _disposed = true;
        });

        _writeCoordinator.Dispose();
        if (uncommitted != null)
            throw uncommitted;
    }

    protected virtual ScopedClientFactory GetClientFactory()
    {
        return new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(_clientFactory, _provider)), null);
    }

    protected virtual IPublishEndpointProvider GetPublishEndpointProvider() => _bus;
    protected virtual ISendEndpointProvider GetSendEndpointProvider() => _bus;

    void EnsureStagedRecordsPending()
    {
        var trackedPending = new HashSet<DurableSendRecord>(ReferenceEqualityComparer.Instance);
        foreach (EntityEntry<DurableSendRecord> entry in _dbContext.ChangeTracker.Entries<DurableSendRecord>())
        {
            if (entry.State == EntityState.Added)
                trackedPending.Add(entry.Entity);
        }

        foreach ((Guid id, StagedSend staged) in _staged)
        {
            DurableSendRecord record = staged.Entry.Entity;
            if (!trackedPending.Contains(record)
                || record.Id != id
                || record.StoreKey != _persistenceIdentity
                || record.GenerationToken != staged.GenerationToken
                || record.StorageSize != staged.StorageSize)
            {
                throw new InvalidOperationException(
                    "The transactional outbox has staged records that are no longer pending under this session and cannot be committed.");
            }
        }

        if (_capacityBaseline is not { } baseline || _staged.Count == 0)
        {
            if (_staged.Count > 0)
                throw new InvalidOperationException("The transactional outbox capacity reservation is missing.");
            return;
        }

        bool trackedCapacity = _dbContext.ChangeTracker.Entries<DurableSendCapacityState>()
            .Any(entry => ReferenceEquals(entry.Entity, baseline.Entry.Entity));
        int expectedCount = checked(baseline.Count + _staged.Count);
        long expectedBytes = checked(baseline.Bytes + _staged.Values.Sum(staged => staged.StorageSize));
        EntityState expectedState = baseline.State == EntityState.Added ? EntityState.Added : EntityState.Modified;
        if (!trackedCapacity || baseline.Entry.State != expectedState
            || baseline.Entry.Entity.StoreKey != _persistenceIdentity
            || baseline.Entry.Entity.StoredCount != expectedCount
            || baseline.Entry.Entity.StoredBytes != expectedBytes)
            throw new InvalidOperationException("The transactional outbox capacity reservation is no longer pending under this session.");
    }

    void OnSavingChanges(object? sender, SavingChangesEventArgs eventArgs)
    {
        _saveIncludedStaged = false;
        EnsureStagedRecordsPending();
        _saveIncludedStaged = _staged.Count > 0;
    }

    void OnSavedChanges(object? sender, SavedChangesEventArgs eventArgs)
    {
        if (_saveIncludedStaged && eventArgs.EntitiesSavedCount >= _staged.Count + 1)
        {
            bool exactEntriesRemain = _staged.Values.All(staged =>
                staged.Entry.State is EntityState.Added or EntityState.Unchanged);
            bool capacityRemains = _capacityBaseline?.Entry.State is EntityState.Added or EntityState.Modified or EntityState.Unchanged;
            if (exactEntriesRemain && capacityRemains)
            {
                foreach (StagedSend staged in _staged.Values)
                    staged.Entry.State = EntityState.Unchanged;
                _capacityBaseline!.Entry.State = EntityState.Unchanged;
                _staged.Clear();
                _capacityBaseline = null;
            }
        }

        _saveIncludedStaged = false;
    }

    void OnSaveChangesFailed(object? sender, SaveChangesFailedEventArgs eventArgs)
    {
        _saveIncludedStaged = false;
    }

    void DetachPendingOutbox()
    {
        var tracked = new HashSet<DurableSendRecord>(ReferenceEqualityComparer.Instance);
        foreach (EntityEntry<DurableSendRecord> entry in _dbContext.ChangeTracker.Entries<DurableSendRecord>())
            tracked.Add(entry.Entity);

        foreach (StagedSend staged in _staged.Values)
        {
            if (tracked.Contains(staged.Entry.Entity))
                staged.Entry.State = EntityState.Detached;
        }

        if (_capacityBaseline is { } baseline
            && _dbContext.ChangeTracker.Entries<DurableSendCapacityState>()
                .Any(entry => ReferenceEquals(entry.Entity, baseline.Entry.Entity)))
        {
            baseline.Entry.Entity.StoredCount = baseline.Count;
            baseline.Entry.Entity.StoredBytes = baseline.Bytes;
            if (baseline.State == EntityState.Added)
                baseline.Entry.State = EntityState.Added;
            else
            {
                baseline.Entry.Property(nameof(DurableSendCapacityState.StoredCount)).OriginalValue = baseline.OriginalCount;
                baseline.Entry.Property(nameof(DurableSendCapacityState.StoredBytes)).OriginalValue = baseline.OriginalBytes;
                baseline.Entry.State = baseline.State;
                baseline.Entry.Property(nameof(DurableSendCapacityState.StoredCount)).IsModified = baseline.CountWasModified;
                baseline.Entry.Property(nameof(DurableSendCapacityState.StoredBytes)).IsModified = baseline.BytesWasModified;
            }
        }

        _staged.Clear();
        _capacityBaseline = null;
        _saveIncludedStaged = false;
    }

    async Task<CapacityReservation> ReserveCapacityAsync(long bytes, CancellationToken cancellationToken)
    {
        if (bytes > _options.MaximumStoredBytes)
        {
            throw new DurableSendCapacityExceededException(
                $"Reliable outbox message size {bytes} exceeds the configured byte limit {_options.MaximumStoredBytes}.",
                0,
                0);
        }

        DurableSendCapacityState? capacity = _dbContext.ChangeTracker.Entries<DurableSendCapacityState>()
            .Select(entry => entry.Entity)
            .SingleOrDefault(state => string.Equals(state.StoreKey, _persistenceIdentity, StringComparison.Ordinal));
        capacity ??= await _dbContext.Set<DurableSendCapacityState>()
            .SingleOrDefaultAsync(state => state.StoreKey == _persistenceIdentity, cancellationToken)
            .ConfigureAwait(false);
        if (capacity is null)
        {
            capacity = new DurableSendCapacityState { StoreKey = _persistenceIdentity };
            _dbContext.Add(capacity);
        }

        EntityEntry<DurableSendCapacityState> capacityEntry = _dbContext.Entry(capacity);
        int previousCount = capacity.StoredCount;
        long previousBytes = capacity.StoredBytes;

        int nextCount = checked(capacity.StoredCount + 1);
        long nextBytes = checked(capacity.StoredBytes + bytes);
        if (nextCount > _options.MaximumStoredCount || nextBytes > _options.MaximumStoredBytes)
        {
            throw new DurableSendCapacityExceededException(
                $"Reliable messaging storage capacity would be exceeded ({nextCount}/{_options.MaximumStoredCount} records, "
                + $"{nextBytes}/{_options.MaximumStoredBytes} bytes).",
                capacity.StoredCount,
                capacity.StoredBytes);
        }

        _capacityBaseline ??= new CapacityBaseline(
            capacityEntry,
            previousCount,
            previousBytes,
            capacityEntry.Property(nameof(DurableSendCapacityState.StoredCount)).OriginalValue is int originalCount ? originalCount : previousCount,
            capacityEntry.Property(nameof(DurableSendCapacityState.StoredBytes)).OriginalValue is long originalBytes ? originalBytes : previousBytes,
            capacityEntry.State,
            capacityEntry.Property(nameof(DurableSendCapacityState.StoredCount)).IsModified,
            capacityEntry.Property(nameof(DurableSendCapacityState.StoredBytes)).IsModified);
        capacity.StoredCount = nextCount;
        capacity.StoredBytes = nextBytes;
        return new CapacityReservation(capacity, previousCount, previousBytes);
    }

    void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    sealed record StagedSend(EntityEntry<DurableSendRecord> Entry, long StorageSize, Guid GenerationToken);
    sealed record CapacityReservation(DurableSendCapacityState Capacity, int PreviousCount, long PreviousBytes);
    sealed record CapacityBaseline(EntityEntry<DurableSendCapacityState> Entry, int Count, long Bytes, int OriginalCount,
        long OriginalBytes, EntityState State, bool CountWasModified, bool BytesWasModified);
}
