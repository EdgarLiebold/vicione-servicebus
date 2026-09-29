using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal class EntityFrameworkTransactionalScopedBusContext<TBus, TDbContext> :
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
    readonly IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>> _notification;
    readonly string _persistenceIdentity;
    readonly IServiceProvider _provider;
    readonly TimeProvider _timeProvider;
    readonly EntityFrameworkOutboxWriteCoordinator _writeCoordinator = new();
    bool _disposed;
    Guid _outboxId = NewId.NextGuid();
    DateTimeOffset _outboxCreated;
    EntityEntry<OutboxState>? _outboxState;
    readonly List<EntityEntry<OutboxMessage>> _stagedMessages = [];
    bool _saveIncludedSession;
    IPublishEndpoint? _publishEndpoint;
    IScopedClientFactory? _scopedClientFactory;
    ISendEndpointProvider? _sendEndpointProvider;

    internal bool HasActiveSession => _outboxState != null;

    public EntityFrameworkTransactionalScopedBusContext(
        TBus bus,
        TDbContext dbContext,
        IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>> notification,
        IClientFactory clientFactory,
        IServiceProvider provider,
        TimeProvider timeProvider,
        BusPersistenceIdentity<TBus> persistenceIdentity)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _notification = notification ?? throw new ArgumentNullException(nameof(notification));
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _persistenceIdentity = (persistenceIdentity ?? throw new ArgumentNullException(nameof(persistenceIdentity)))
            .Require("Entity Framework transactional outbox");
        _dbContext.SavingChanges += OnSavingChanges;
        _dbContext.SavedChanges += OnSavedChanges;
        _dbContext.SaveChangesFailed += OnSaveChangesFailed;
    }

    public ISendEndpointProvider SendEndpointProvider =>
        _sendEndpointProvider ??= new OutboxSendEndpointProvider(this, GetSendEndpointProvider());

    public IPublishEndpoint PublishEndpoint =>
        _publishEndpoint ??= new PublishEndpoint(new OutboxPublishEndpointProvider(this, GetPublishEndpointProvider()));

    public IScopedClientFactory ClientFactory => _scopedClientFactory ??= GetClientFactory();

    public object? GetService(Type serviceType) => _provider.GetService(serviceType);

    public Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ThrowIfDisposed();

        return _writeCoordinator.ExecuteAsync(() =>
        {
            ThrowIfDisposed();
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

            MessageBody admittedBody = PayloadAdmissionTransportBoundary.Admit(admissionRuntime, context);
            var message = OutboxMessageFactory.Create(
                context,
                ServiceBusMetadataJson.ObjectDeserializer,
                _timeProvider,
                outboxId: _outboxId,
                admittedBody: admittedBody);
            bool hadSession = _outboxState != null;
            try
            {
                EnsureOutboxState();
                _stagedMessages.Add(_dbContext.Add(message));
            }
            catch
            {
                DetachIfTracked(message);
                if (!hadSession && _stagedMessages.Count == 0)
                    DetachPendingOutbox();
                throw;
            }
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _writeCoordinator.ExecuteAsync(async () =>
        {
            ThrowIfDisposed();
            EnsureSessionPending();

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (_outboxState == null)
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
            if (_outboxState != null)
            {
                Guid abandonedOutboxId = _outboxId;
                try
                {
                    DetachPendingOutbox();
                }
                catch (ObjectDisposedException)
                {
                    _outboxState = null;
                    _outboxId = NewId.NextGuid();
                }

                uncommitted = new InvalidOperationException(
                    $"The transactional outbox for {TypeCache<TBus>.ShortName}/{TypeCache<TDbContext>.ShortName} was disposed without commit. "
                    + $"Staged outbox {abandonedOutboxId} records were discarded to prevent accidental later persistence.");
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

    protected virtual ScopedClientFactory GetClientFactory() =>
        new(new ClientFactory(new ScopedClientFactoryContext(_clientFactory, _provider)), null);

    protected virtual IPublishEndpointProvider GetPublishEndpointProvider() => _bus;

    protected virtual ISendEndpointProvider GetSendEndpointProvider() => _bus;

    void EnsureOutboxState()
    {
        if (_outboxState != null)
        {
            EnsureSessionPending();
            return;
        }

        _outboxCreated = _timeProvider.GetUtcNow();
        var state = new OutboxState
        {
            OutboxId = _outboxId,
            BusKey = _persistenceIdentity,
            Created = _outboxCreated,
            Status = OutboxDeliveryStatus.Pending,
        };
        try
        {
            _outboxState = _dbContext.Add(state);
        }
        catch
        {
            DetachIfTracked(state);
            throw;
        }
    }

    void EnsureSessionPending()
    {
        if (_outboxState is null)
            return;

        EnsureStateOwnedBySession();
        EnsureDeliveryStateUnchanged();
        EnsureMessagesOwnedBySession();
    }

    void EnsureStateOwnedBySession()
    {
        bool stateTracked = _dbContext.ChangeTracker.Entries<OutboxState>()
            .Any(entry => ReferenceEquals(entry.Entity, _outboxState!.Entity));
        if (!stateTracked || _outboxState!.State != EntityState.Added
            || _outboxState.Entity.OutboxId != _outboxId
            || _outboxState.Entity.BusKey != _persistenceIdentity
            || _stagedMessages.Count == 0)
            throw new InvalidOperationException("The transactional outbox has staged state that is no longer pending under this session.");
    }

    void EnsureDeliveryStateUnchanged()
    {
        OutboxState state = _outboxState!.Entity;
        if (state.Created != _outboxCreated
            || state.Status != OutboxDeliveryStatus.Pending
            || state.LockId != Guid.Empty
            || state.RowVersion is not null
            || state.NextDeliveryTime is not null
            || state.DeliveryAttempts != 0
            || state.LastFailureKind != OutboxFailureKind.None
            || state.LastFailureCode != OutboxFailureCode.None
            || state.LastFailureTime is not null
            || state.LastExceptionType is not null
            || state.FailedSequenceNumber is not null
            || state.FailedMessageId is not null
            || state.Delivered is not null
            || state.LastSequenceNumber is not null)
            throw new InvalidOperationException("The transactional outbox delivery state was changed before persistence.");
    }

    void EnsureMessagesOwnedBySession()
    {
        var tracked = new HashSet<OutboxMessage>(
            _dbContext.ChangeTracker.Entries<OutboxMessage>().Select(entry => entry.Entity),
            ReferenceEqualityComparer.Instance);
        foreach (EntityEntry<OutboxMessage> entry in _stagedMessages)
        {
            if (!tracked.Contains(entry.Entity) || entry.State != EntityState.Added
                || entry.Entity.OutboxId != _outboxId)
                throw new InvalidOperationException("The transactional outbox has staged messages that are no longer pending under this session.");
        }
    }

    void OnSavingChanges(object? sender, SavingChangesEventArgs eventArgs)
    {
        _saveIncludedSession = false;
        EnsureSessionPending();
        _saveIncludedSession = _outboxState != null;
    }

    void OnSavedChanges(object? sender, SavedChangesEventArgs eventArgs)
    {
        if (_saveIncludedSession && _outboxState is not null
            && eventArgs.EntitiesSavedCount >= _stagedMessages.Count + 1
            && _outboxState.State is EntityState.Added or EntityState.Unchanged
            && _stagedMessages.All(entry => entry.State is EntityState.Added or EntityState.Unchanged))
            CompleteCommittedOutbox();
        _saveIncludedSession = false;
    }

    void OnSaveChangesFailed(object? sender, SaveChangesFailedEventArgs eventArgs)
    {
        _saveIncludedSession = false;
    }

    void CompleteCommittedOutbox()
    {
        if (_outboxState == null)
            return;

        foreach (EntityEntry<OutboxMessage> entry in _stagedMessages)
            entry.State = EntityState.Unchanged;
        _outboxState.State = EntityState.Unchanged;
        _stagedMessages.Clear();
        _outboxState = null;
        _outboxId = NewId.NextGuid();
        _notification.SignalDelivery();
    }

    void DetachPendingOutbox()
    {
        var tracked = new HashSet<OutboxMessage>(
            _dbContext.ChangeTracker.Entries<OutboxMessage>().Select(entry => entry.Entity),
            ReferenceEqualityComparer.Instance);
        foreach (EntityEntry<OutboxMessage> entry in _stagedMessages)
        {
            if (tracked.Contains(entry.Entity))
                entry.State = EntityState.Detached;
        }

        if (_outboxState != null && _dbContext.ChangeTracker.Entries<OutboxState>()
            .Any(entry => ReferenceEquals(entry.Entity, _outboxState.Entity)))
            _outboxState.State = EntityState.Detached;

        _stagedMessages.Clear();
        _outboxState = null;
        _outboxId = NewId.NextGuid();
        _saveIncludedSession = false;
    }

    void DetachIfTracked<TEntity>(TEntity entity) where TEntity : class
    {
        if (_dbContext.ChangeTracker.Entries<TEntity>()
            .Any(entry => ReferenceEquals(entry.Entity, entity)))
            _dbContext.Entry(entity).State = EntityState.Detached;
    }

    void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
