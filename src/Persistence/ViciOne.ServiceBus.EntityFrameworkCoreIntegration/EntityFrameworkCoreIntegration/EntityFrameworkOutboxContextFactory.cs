using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public class EntityFrameworkOutboxContextFactory<TDbContext> :
    IOutboxContextFactory<TDbContext>
    where TDbContext : DbContext
{
    readonly TDbContext _dbContext;
    readonly IsolationLevel _isolationLevel;
    readonly ILockStatementProvider _lockStatementProvider;
    readonly IServiceProvider _provider;
    readonly TimeProvider _timeProvider;
    string? _lockStatement;

    public EntityFrameworkOutboxContextFactory(TDbContext dbContext, IServiceProvider provider, IOptions<EntityFrameworkOutboxOptions<TDbContext>> options,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _provider = provider;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _lockStatementProvider = options.Value.LockStatementProvider;
        _isolationLevel = options.Value.IsolationLevel;
    }

    public async Task SendAsync<T>(ConsumeContext<T> context, OutboxConsumeOptions options, IPipe<OutboxConsumeContext<T>> next, CancellationToken cancellationToken = default)
        where T : class
    {
        var messageId = context.GetOriginalMessageId() ?? throw new MessageException(typeof(T), "MessageId required to use the outbox");
        var updateDeliveryCount = true;

        _lockStatement ??= _lockStatementProvider.GetRowLockStatement<InboxState>(_dbContext, nameof(InboxState.MessageId), nameof(InboxState.ConsumerId));

        async Task<bool> ExecuteAsync()
        {
            var lockId = NewId.NextGuid();

            long startedAt = _timeProvider.GetTimestamp();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(_isolationLevel, context.CancellationToken)
                .ConfigureAwait(false);

            try
            {
                List<InboxState> inboxStateList = await _dbContext.Set<InboxState>()
                    .FromSqlRaw(_lockStatement, messageId, options.ConsumerId)
                    .AsTracking()
                    .ToListAsync(context.CancellationToken).ConfigureAwait(false);
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

                    await _dbContext.AddAsync(inboxState, context.CancellationToken).ConfigureAwait(false);
                    await _dbContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

                    continueProcessing = true;
                }
                else
                {
                    inboxState.LockId = lockId;
                    if (updateDeliveryCount)
                        inboxState.ReceiveCount++;

                    _dbContext.Update(inboxState);
                    await _dbContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

                    var outboxContext = new DbContextOutboxConsumeContext<TDbContext, T>(context, options, _provider, _dbContext, transaction, inboxState,
                        _timeProvider);

                    await next.SendAsync(outboxContext).ConfigureAwait(false);

                    try
                    {
                        await _dbContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        await context.NotifyFaultedAsync(_timeProvider.GetElapsedTime(startedAt), TypeCache<T>.ShortName, exception, cancellationToken: cancellationToken).ConfigureAwait(false);

                        throw;
                    }

                    continueProcessing = outboxContext.ContinueProcessing;
                }

                try
                {
                    await transaction.CommitAsync(context.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    await context.NotifyFaultedAsync(_timeProvider.GetElapsedTime(startedAt), TypeCache<T>.ShortName, exception, cancellationToken: cancellationToken).ConfigureAwait(false);

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
                    //
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
                    context.CancellationToken)
                .ConfigureAwait(false);
            updateDeliveryCount = false;
        }
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("outboxContextFactory");
        scope.Add("provider", "entityFrameworkCore");
    }
}
