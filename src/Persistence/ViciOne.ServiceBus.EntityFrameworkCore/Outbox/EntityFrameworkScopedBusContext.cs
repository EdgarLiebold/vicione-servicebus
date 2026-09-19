using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    readonly HashSet<Guid> _stagedIds = [];
    bool _disposed;
    IPublishEndpoint? _publishEndpoint;
    IScopedClientFactory? _scopedClientFactory;
    ISendEndpointProvider? _sendEndpointProvider;

    internal bool HasActiveSession => _stagedIds.Count > 0;

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
        _dbContext.SavedChanges += OnSavedChanges;
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
            if (WasCommitted())
                CompleteCommittedOutbox();

            if (!context.MessageId.HasValue)
                throw new MessageException(typeof(T), "The SendContext MessageId must be present");
            Uri destination = context.DestinationAddress
                ?? throw new MessageException(typeof(T), "The SendContext DestinationAddress must be present");
            DateTimeOffset now = _timeProvider.GetUtcNow();
            PayloadAdmissionRuntime<TBus>? admissionRuntime = _provider.GetService<PayloadAdmissionRuntime<TBus>>();
            if (admissionRuntime is null)
            {
                throw new ConfigurationException(
                    $"The Entity Framework transactional outbox for bus '{typeof(TBus)}' has no payload-admission runtime.");
            }

            byte[] body = PayloadAdmissionTransportBoundary.Admit(admissionRuntime, context).ToArray();
            string contentType = context.ContentType?.ToString() ?? context.Serialization.DefaultContentType.ToString();
            if (!context.TryGetPayload(out PayloadAdmissionSerializationContext? admission)
                || !admission.TryCreateDurableProof(contentType, out DurablePayloadAdmissionProof proof)
                || !proof.MatchesEnvelope(body, contentType))
                throw new InvalidOperationException("The transactional outbox has no complete payload admission proof for its serialized envelope.");

            byte[] metadata = ReliableEnvelopeMetadataCodec.Capture(context, now, proof).ToArray();
            Guid id = context.MessageId.Value;
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

            await ReserveCapacityAsync(record.StorageSize, cancellationToken).ConfigureAwait(false);
            _dbContext.Add(record);
            _stagedIds.Add(id);
        }, cancellationToken);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _writeCoordinator.ExecuteAsync(async () =>
        {
            ThrowIfDisposed();
            if (WasCommitted())
            {
                CompleteCommittedOutbox();
                return;
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (_stagedIds.Count == 0)
                return;

            if (!WasCommitted())
                throw new InvalidOperationException("The transactional outbox SaveChanges operation did not persist the staged outbox state.");

            CompleteCommittedOutbox();
        }, cancellationToken);
    }

    public Task AbortAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _writeCoordinator.ExecuteAsync(() =>
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (WasCommitted())
            {
                CompleteCommittedOutbox();
                throw new InvalidOperationException("A persisted transactional outbox session cannot be aborted.");
            }

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
            if (_stagedIds.Count > 0)
            {
                Guid[] abandonedIds = _stagedIds.ToArray();
                try
                {
                    DetachPendingOutbox();
                }
                catch (ObjectDisposedException)
                {
                    _stagedIds.Clear();
                }

                uncommitted = new InvalidOperationException(
                    $"The transactional outbox for {TypeCache<TBus>.ShortName}/{TypeCache<TDbContext>.ShortName} was disposed without commit. "
                    + $"{abandonedIds.Length} staged outbox records were discarded to prevent accidental later persistence.");
            }

            _dbContext.SavedChanges -= OnSavedChanges;
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

    bool WasCommitted() => _stagedIds.Count > 0
        && _dbContext.ChangeTracker.Entries<DurableSendRecord>()
            .Where(entry => _stagedIds.Contains(entry.Entity.Id))
            .All(entry => entry.State is EntityState.Unchanged or EntityState.Detached);

    void OnSavedChanges(object? sender, SavedChangesEventArgs eventArgs)
    {
        CompleteCommittedOutbox();
    }

    void CompleteCommittedOutbox()
    {
        if (!WasCommitted())
            return;

        _stagedIds.Clear();
    }

    void DetachPendingOutbox()
    {
        long releasedBytes = 0;
        int releasedCount = 0;
        foreach (var entry in _dbContext.ChangeTracker.Entries<DurableSendRecord>()
                     .Where(entry => _stagedIds.Contains(entry.Entity.Id))
                     .ToArray())
        {
            if (entry.State != EntityState.Unchanged)
            {
                releasedBytes = checked(releasedBytes + entry.Entity.StorageSize);
                releasedCount++;
                entry.State = EntityState.Detached;
            }
        }

        DurableSendCapacityState? capacity = _dbContext.ChangeTracker.Entries<DurableSendCapacityState>()
            .Select(entry => entry.Entity)
            .SingleOrDefault(state => string.Equals(state.StoreKey, _persistenceIdentity, StringComparison.Ordinal));
        if (capacity is not null && releasedCount > 0)
        {
            capacity.StoredCount = checked(capacity.StoredCount - releasedCount);
            capacity.StoredBytes = checked(capacity.StoredBytes - releasedBytes);
        }

        _stagedIds.Clear();
    }

    async Task ReserveCapacityAsync(long bytes, CancellationToken cancellationToken)
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

        capacity.StoredCount = nextCount;
        capacity.StoredBytes = nextBytes;
    }

    void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
