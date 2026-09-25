using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class EntityFrameworkReliableInboxContextFactory<TBus, TDbContext> :
    IOutboxContextFactory<EntityFrameworkReliableInboxScope<TBus, TDbContext>>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    readonly TDbContext _dbContext;
    readonly IDbContextFactory<TDbContext> _dbContextFactory;
    readonly ILogger<EntityFrameworkReliableInboxContextFactory<TBus, TDbContext>> _logger;
    readonly ReliableMessagingOptions<TBus> _options;
    readonly EntityFrameworkScopedBusContext<TBus, TDbContext> _outbox;
    readonly IServiceProvider _provider;
    readonly string _storeKey;
    readonly TimeProvider _timeProvider;

    public EntityFrameworkReliableInboxContextFactory(
        TDbContext dbContext,
        IDbContextFactory<TDbContext> dbContextFactory,
        EntityFrameworkScopedBusContext<TBus, TDbContext> outbox,
        IServiceProvider provider,
        IOptions<ReliableMessagingOptions<TBus>> options,
        BusPersistenceIdentity<TBus> persistenceIdentity,
        TimeProvider timeProvider,
        ILogger<EntityFrameworkReliableInboxContextFactory<TBus, TDbContext>> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _storeKey = (persistenceIdentity ?? throw new ArgumentNullException(nameof(persistenceIdentity)))
            .Require("Entity Framework reliable inbox");
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendAsync<T>(
        ConsumeContext<T> context,
        OutboxConsumeOptions options,
        IPipe<OutboxConsumeContext<T>> next,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(next);
        CancellationToken operationCancellationToken = cancellationToken.CanBeCanceled
            ? cancellationToken
            : context.CancellationToken;
        operationCancellationToken.ThrowIfCancellationRequested();
        Guid messageId = context.GetOriginalMessageId()
            ?? throw new MessageException(typeof(T), "MessageId required to use reliable messaging");
        var key = new ReliableInboxKey(messageId, options.ConsumerId).Validate();

        while (true)
        {
            operationCancellationToken.ThrowIfCancellationRequested();
            _dbContext.ChangeTracker.Clear();
            DateTimeOffset now = _timeProvider.GetUtcNow();
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, operationCancellationToken)
                .ConfigureAwait(false);

            ReliableInboxRecord? inbox = await _dbContext.Set<ReliableInboxRecord>()
                .SingleOrDefaultAsync(
                    row => row.StoreKey == _storeKey
                        && row.MessageId == key.MessageId
                        && row.ConsumerId == key.ConsumerId,
                    operationCancellationToken)
                .ConfigureAwait(false);

            if (inbox?.Status is ReliableInboxStatus.Consumed
                or ReliableInboxStatus.Quarantined
                or ReliableInboxStatus.Abandoned)
            {
                await transaction.CommitAsync(operationCancellationToken).ConfigureAwait(false);
                return;
            }

            DateTimeOffset? waitUntil = GetWaitUntil(inbox, now);
            if (waitUntil.HasValue)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                await Task.Delay(waitUntil.Value - now, _timeProvider, operationCancellationToken).ConfigureAwait(false);
                continue;
            }

            Guid leaseToken = Guid.NewGuid();
            bool isNewInbox = inbox is null;
            int attemptedDelivery;
            if (inbox is null)
            {
                attemptedDelivery = 1;
                inbox = new ReliableInboxRecord
                {
                    StoreKey = _storeKey,
                    MessageId = key.MessageId,
                    ConsumerId = key.ConsumerId,
                    Status = ReliableInboxStatus.Processing,
                    Attempts = 1,
                    ReceivedAt = now.UtcDateTime,
                    LeaseToken = leaseToken,
                    LeaseExpiresAt = (now + _options.LeaseDuration).UtcDateTime,
                };
                _dbContext.Add(inbox);
            }
            else
            {
                attemptedDelivery = checked(inbox.Attempts + 1);
                inbox.Status = ReliableInboxStatus.Processing;
                inbox.Attempts = attemptedDelivery;
                inbox.DueAt = null;
                inbox.LeaseToken = leaseToken;
                inbox.LeaseExpiresAt = (now + _options.LeaseDuration).UtcDateTime;
                _dbContext.Update(inbox);
            }

            if (await SendWithLeaseAsync(
                context,
                options,
                next,
                transaction,
                inbox,
                key,
                attemptedDelivery,
                isNewInbox,
                operationCancellationToken).ConfigureAwait(false))
                continue;

            return;
        }
    }

    async Task<bool> SendWithLeaseAsync<T>(
        ConsumeContext<T> context,
        OutboxConsumeOptions options,
        IPipe<OutboxConsumeContext<T>> next,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        ReliableInboxRecord inbox,
        ReliableInboxKey key,
        int attemptedDelivery,
        bool isNewInbox,
        CancellationToken operationCancellationToken)
        where T : class
    {
        bool leasePersisted = false;
        try
        {
            await _dbContext.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
            leasePersisted = true;
            var reliableContext = new EntityFrameworkReliableInboxContext<TBus, TDbContext, T>(
                context,
                options,
                _provider,
                _dbContext,
                inbox,
                _outbox,
                _timeProvider);
            await next.SendAsync(reliableContext).ConfigureAwait(false);
            await transaction.CommitAsync(operationCancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException) when (operationCancellationToken.IsCancellationRequested)
        {
            await RollbackAsync(transaction).ConfigureAwait(false);
            await AbortOutboxAsync().ConfigureAwait(false);
            _dbContext.ChangeTracker.Clear();
            throw;
        }
        catch (DbUpdateException) when (isNewInbox && !leasePersisted)
        {
            await RollbackAsync(transaction).ConfigureAwait(false);
            _dbContext.ChangeTracker.Clear();
            if (await InboxExistsAsync(key, operationCancellationToken).ConfigureAwait(false))
                return true;

            throw;
        }
        catch (Exception exception)
        {
            await RollbackAsync(transaction).ConfigureAwait(false);
            await AbortOutboxAsync().ConfigureAwait(false);
            _dbContext.ChangeTracker.Clear();
            ReliableInboxStatus? retained = await PersistFailureAsync(
                key,
                _timeProvider.GetUtcNow(),
                exception,
                attemptedDelivery).ConfigureAwait(false);
            if (retained is ReliableInboxStatus.Consumed
                or ReliableInboxStatus.Quarantined
                or ReliableInboxStatus.Abandoned)
            {
                return false;
            }

            if (retained == ReliableInboxStatus.RetryScheduled)
                throw new ReliableInboxRetryRequiredException(exception);

            throw;
        }
    }

    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateFilterScope("reliableInbox");
        scope.Add("provider", "entityFrameworkCore");
    }

    DateTimeOffset? GetWaitUntil(ReliableInboxRecord? inbox, DateTimeOffset now)
    {
        if (inbox?.Status == ReliableInboxStatus.RetryScheduled
            && inbox.DueAt is { } due
            && due > now.UtcDateTime)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(due, DateTimeKind.Utc));
        }

        if (inbox?.Status == ReliableInboxStatus.Processing
            && inbox.LeaseExpiresAt is { } expires
            && expires > now.UtcDateTime)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(expires, DateTimeKind.Utc));
        }

        return null;
    }

    async Task<ReliableInboxStatus?> PersistFailureAsync(
        ReliableInboxKey key,
        DateTimeOffset failedAt,
        Exception exception,
        int attemptedDelivery)
    {
        try
        {
            await using TDbContext db = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None).ConfigureAwait(false);
            await using var transaction = await db.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, CancellationToken.None)
                .ConfigureAwait(false);
            ReliableInboxRecord? inbox = await db.Set<ReliableInboxRecord>().SingleOrDefaultAsync(
                row => row.StoreKey == _storeKey
                    && row.MessageId == key.MessageId
                    && row.ConsumerId == key.ConsumerId,
                CancellationToken.None).ConfigureAwait(false);

            if (inbox?.Status is ReliableInboxStatus.Consumed
                or ReliableInboxStatus.Quarantined
                or ReliableInboxStatus.Abandoned)
            {
                await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                return inbox.Status;
            }

            int attempts = Math.Max(attemptedDelivery, inbox?.Attempts ?? 0);
            ReliableInboxStatus status = attempts >= _options.MaximumDeliveryAttempts
                ? ReliableInboxStatus.Quarantined
                : ReliableInboxStatus.RetryScheduled;
            DateTimeOffset? dueAt = status == ReliableInboxStatus.RetryScheduled
                ? failedAt + CalculateRetryDelay(attempts)
                : null;
            string failureType = GetFailureTypeName(exception);

            if (inbox is null)
            {
                inbox = new ReliableInboxRecord
                {
                    StoreKey = _storeKey,
                    MessageId = key.MessageId,
                    ConsumerId = key.ConsumerId,
                    Status = status,
                    Attempts = attempts,
                    ReceivedAt = failedAt.UtcDateTime,
                };
                db.Add(inbox);
            }

            inbox.Attempts = attempts;
            inbox.Status = status;
            inbox.DueAt = dueAt?.UtcDateTime;
            inbox.LeaseToken = null;
            inbox.LeaseExpiresAt = null;
            inbox.FailedAt = failedAt.UtcDateTime;
            inbox.QuarantinedAt = status == ReliableInboxStatus.Quarantined ? failedAt.UtcDateTime : null;
            inbox.FailureType = failureType;
            await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            return status;
        }
        catch (Exception persistenceException)
        {
            // The original consumer failure is authoritative. A failed retry-state write must remain visible without
            // converting the delivery into success or masking the business exception.
            try
            {
                _logger.LogError(
                    persistenceException,
                    "Reliable inbox could not persist failure state for {StoreKey}/{MessageId}/{ConsumerId}",
                    _storeKey,
                    key.MessageId,
                    key.ConsumerId);
            }
            catch
            {
                // Logging is a no-throw boundary.
            }

            return null;
        }
    }

    async Task AbortOutboxAsync()
    {
        try
        {
            await _outbox.AbortAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // A commit that raced with cancellation is resolved by the authoritative persisted inbox state below.
        }
    }

    async Task<bool> InboxExistsAsync(ReliableInboxKey key, CancellationToken cancellationToken)
    {
        await using TDbContext db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Set<ReliableInboxRecord>().AsNoTracking().AnyAsync(
            row => row.StoreKey == _storeKey
                && row.MessageId == key.MessageId
                && row.ConsumerId == key.ConsumerId,
            cancellationToken).ConfigureAwait(false);
    }

    TimeSpan CalculateRetryDelay(int attempt)
    {
        long ticks = _options.InitialRetryDelay.Ticks;
        long maximumTicks = _options.MaximumRetryDelay.Ticks;
        for (var index = 1; index < attempt && ticks < maximumTicks; index++)
            ticks = ticks > maximumTicks / 2 ? maximumTicks : Math.Min(maximumTicks, ticks * 2);
        return TimeSpan.FromTicks(ticks);
    }

    static string GetFailureTypeName(Exception exception)
    {
        Type exceptionType = exception.GetType();
        string typeName = exceptionType.FullName ?? exceptionType.Name;
        return typeName.Length <= 512 ? typeName : typeName[..512];
    }

    static async Task RollbackAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Preserve the authoritative consumer failure.
        }
    }
}
