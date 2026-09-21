using System;
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
    EntityEntry<OutboxState>? _outboxState;
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
        _dbContext.SavedChanges += OnSavedChanges;
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
            EnsureOutboxState();

            var message = OutboxMessageFactory.Create(
                context,
                ServiceBusMetadataJson.ObjectDeserializer,
                _timeProvider,
                outboxId: _outboxId,
                admittedBody: admittedBody);
            _dbContext.Add(message);
            return Task.CompletedTask;
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
            if (_outboxState == null)
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

            _dbContext.SavedChanges -= OnSavedChanges;
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
        if (_outboxState != null && !WasCommitted())
            return;

        if (WasCommitted())
            CompleteCommittedOutbox();

        _outboxId = NewId.NextGuid();
        _outboxState = _dbContext.Add(new OutboxState
        {
            OutboxId = _outboxId,
            BusKey = _persistenceIdentity,
            Created = _timeProvider.GetUtcNow().UtcDateTime,
            Status = OutboxDeliveryStatus.Pending,
        });
    }

    bool WasCommitted() => _outboxState?.State == EntityState.Unchanged;

    void OnSavedChanges(object? sender, SavedChangesEventArgs eventArgs)
    {
        CompleteCommittedOutbox();
    }

    void CompleteCommittedOutbox()
    {
        if (_outboxState == null || !WasCommitted())
            return;

        _notification.SignalDelivery();
        _outboxState = null;
        _outboxId = NewId.NextGuid();
    }

    void DetachPendingOutbox()
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<OutboxMessage>()
                     .Where(entry => entry.Entity.OutboxId == _outboxId)
                     .ToArray())
        {
            if (entry.State != EntityState.Unchanged)
                entry.State = EntityState.Detached;
        }

        if (_outboxState is { State: not EntityState.Unchanged })
            _outboxState.State = EntityState.Detached;

        _outboxState = null;
        _outboxId = NewId.NextGuid();
    }

    void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
