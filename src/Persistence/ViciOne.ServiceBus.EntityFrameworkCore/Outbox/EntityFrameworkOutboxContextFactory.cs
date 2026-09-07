using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Creates receive-side EF Core inbox/outbox transactions with provider-specific row locking.</summary>
/// <typeparam name="TDbContext">The db context type.</typeparam>
internal sealed class EntityFrameworkOutboxContextFactory<TDbContext> :
    IOutboxContextFactory<TDbContext>
    where TDbContext : DbContext
{
    readonly TDbContext _dbContext;
    readonly IsolationLevel _isolationLevel;
    readonly ILockStatementProvider _lockStatementProvider;
    readonly IServiceProvider _provider;
    readonly TimeProvider _timeProvider;
    string? _lockStatement;

    /// <summary>Initializes the factory for a scoped DbContext.</summary>
    /// <param name="dbContext">The DbContext that stores inbox and outgoing-message rows.</param>
    /// <param name="provider">The scoped service provider exposed to outbox contexts.</param>
    /// <param name="options">The configured transaction isolation and lock-statement provider.</param>
    /// <param name="timeProvider">The source for inbox and diagnostic timestamps.</param>
    public EntityFrameworkOutboxContextFactory(TDbContext dbContext, IServiceProvider provider, IOptions<EntityFrameworkOutboxOptions<TDbContext>> options,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        EntityFrameworkOutboxOptions<TDbContext> value =
            (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _lockStatementProvider = value.LockStatementProvider
            ?? throw new ArgumentException("A lock-statement provider is required.", nameof(options));
        _isolationLevel = Enum.IsDefined(value.IsolationLevel)
            ? value.IsolationLevel
            : throw new ArgumentOutOfRangeException(nameof(options), value.IsolationLevel, "The transaction isolation level is undefined.");
    }

    /// <summary>Runs the receive pipeline within a transaction locked by message and consumer identity.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The active consumption context.</param>
    /// <param name="options">The consumer identity and receive-side outbox limits.</param>
    /// <param name="next">The outbox pipeline to execute after the inbox row is loaded.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, OutboxConsumeOptions options, IPipe<OutboxConsumeContext<T>> next, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(next);
        CancellationToken operationCancellationToken = cancellationToken.CanBeCanceled
            ? cancellationToken
            : context.CancellationToken;
        operationCancellationToken.ThrowIfCancellationRequested();
        var messageId = context.GetOriginalMessageId() ?? throw new MessageException(typeof(T), "MessageId required to use the outbox");
        var updateDeliveryCount = true;

        _lockStatement ??= _lockStatementProvider.GetRowLockStatement<InboxState>(_dbContext, nameof(InboxState.MessageId), nameof(InboxState.ConsumerId));

        async Task<bool> ExecuteAsync()
        {
            foreach (var tracked in _dbContext.ChangeTracker.Entries<InboxState>()
                         .Where(entry => entry.Entity.MessageId == messageId && entry.Entity.ConsumerId == options.ConsumerId)
                         .ToArray())
            {
                tracked.State = EntityState.Detached;
            }

            var lockId = NewId.NextGuid();

            long startedAt = _timeProvider.GetTimestamp();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(_isolationLevel, operationCancellationToken)
                .ConfigureAwait(false);

            try
            {
                List<InboxState> inboxStateList = await _dbContext.Set<InboxState>()
                    .FromSqlRaw(_lockStatement, messageId, options.ConsumerId)
                    .AsTracking()
                    .ToListAsync(operationCancellationToken).ConfigureAwait(false);
                var inboxState = inboxStateList.SingleOrDefault();

                bool continueProcessing;

                if (inboxState == null)
                {
                    inboxState = new InboxState
                    {
                        MessageId = messageId,
                        ConsumerId = options.ConsumerId,
                        Received = _timeProvider.GetUtcNow().UtcDateTime,
                        LockId = lockId,
                        ReceiveCount = 1
                    };

                    await _dbContext.AddAsync(inboxState, operationCancellationToken).ConfigureAwait(false);
                    await _dbContext.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);

                    continueProcessing = true;
                }
                else
                {
                    inboxState.LockId = lockId;
                    if (updateDeliveryCount)
                        inboxState.ReceiveCount++;

                    _dbContext.Update(inboxState);
                    await _dbContext.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);

                    var outboxContext = new DbContextOutboxConsumeContext<TDbContext, T>(context, options, _provider, _dbContext, transaction, inboxState,
                        _timeProvider);

                    await next.SendAsync(outboxContext).ConfigureAwait(false);

                    try
                    {
                        await _dbContext.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        await context.NotifyFaultedAsync(
                            _timeProvider.GetElapsedTime(startedAt),
                            TypeCache<T>.ShortName,
                            exception,
                            cancellationToken: operationCancellationToken).ConfigureAwait(false);

                        throw;
                    }

                    continueProcessing = outboxContext.ContinueProcessing;
                }

                try
                {
                    await transaction.CommitAsync(operationCancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    await context.NotifyFaultedAsync(
                        _timeProvider.GetElapsedTime(startedAt),
                        TypeCache<T>.ShortName,
                        exception,
                        cancellationToken: operationCancellationToken).ConfigureAwait(false);

                    throw;
                }

                return continueProcessing;
            }
            catch (Exception)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Rollback is best effort because the original transaction or pipeline failure is authoritative.
                }

                throw;
            }
        }

        var continueProcessing = true;
        while (continueProcessing)
        {
            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
            continueProcessing = await EntityFrameworkExecutionStrategy.ExecuteAsync(
                    _dbContext,
                    executionStrategy,
                    ExecuteAsync,
                    operationCancellationToken)
                .ConfigureAwait(false);
            updateDeliveryCount = false;
        }
    }

    /// <summary>Adds the EF Core provider identity to the pipeline probe.</summary>
    /// <param name="context">The probe context to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateFilterScope("outboxContextFactory");
        scope.Add("provider", "entityFrameworkCore");
    }
}
