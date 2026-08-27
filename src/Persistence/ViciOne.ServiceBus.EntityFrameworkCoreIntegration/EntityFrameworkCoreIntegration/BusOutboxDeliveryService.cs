namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using Logging;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Middleware;
    using Middleware.Outbox;
    using RetryPolicies;
    using Serialization;
    using Util;


    public class BusOutboxDeliveryService<TDbContext> :
        BackgroundService
        where TDbContext : DbContext
    {
        readonly IBusControl _busControl;
        readonly IsolationLevel _isolationLevel;
        readonly ILockStatementProvider _lockStatementProvider;
        readonly ILogger _logger;
        readonly IBusOutboxNotification _notification;
        readonly OutboxDeliveryServiceOptions _options;
        readonly Func<TDbContext, Guid, long, int, IAsyncEnumerable<OutboxMessage>> _outboxMessagesQuery;
        readonly IServiceProvider _provider;
        readonly IRetryPolicy _retryPolicy;
        readonly TimeProvider _timeProvider;

        string _getOutboxIdStatement;

        public BusOutboxDeliveryService(IBusControl busControl, IOptions<OutboxDeliveryServiceOptions> options,
            IOptions<EntityFrameworkOutboxOptions<TDbContext>> outboxOptions, IBusOutboxNotification notification,
            ILogger<BusOutboxDeliveryService<TDbContext>> logger, IServiceProvider provider, TimeProvider timeProvider)
        {
            _busControl = busControl;
            _notification = notification;
            _provider = provider;
            _logger = logger;
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

            _options = options.Value;

            _lockStatementProvider = outboxOptions.Value.LockStatementProvider;
            _isolationLevel = outboxOptions.Value.IsolationLevel;

            _outboxMessagesQuery = EF.CompileAsyncQuery((TDbContext context, Guid outboxId, long lastSequenceNumber, int limit) =>
                context.Set<OutboxMessage>()
                    .Where(x => x.OutboxId == outboxId && x.SequenceNumber > lastSequenceNumber)
                    .OrderBy(x => x.SequenceNumber)
                    .Take(limit)
                    .AsNoTracking());

            _retryPolicy = Retry.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // A background service starts on the execution context of the host, which carries no log context of this
            // provider. It is established here, before the first await, so that the outbox delivery instruments
            // record through the meter scope of this provider and not through whatever happened to be ambient.
            LogContext.ConfigureCurrentLogContextIfNull(_provider);

            using var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
            {
                PrefetchCount = _options.QueryMessageLimit,
                RequestResultLimit = 10,
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _busControl.WaitForHealthStatusAsync(BusHealthStatus.Healthy, stoppingToken).ConfigureAwait(false);

                    // ReSharper disable once AccessToDisposedClosure
                    var count = await _retryPolicy.Retry(() => algorithm.Run(DeliverOutbox, stoppingToken), stoppingToken).ConfigureAwait(false);
                    if (count > 0)
                        continue;

                    await _notification.WaitForDelivery(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (DbUpdateConcurrencyException)
                {
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "ProcessMessageBatch faulted");
                }
            }
        }

        async Task<int> DeliverOutbox(int resultLimit, CancellationToken cancellationToken)
        {
            var scope = _provider.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

            try
            {
                _getOutboxIdStatement ??= _lockStatementProvider.GetOutboxStatement(dbContext);

                async Task<int> Execute()
                {
                    var lockId = NewId.NextGuid();

                    using var timeoutToken = new CancellationTokenSource(_options.QueryTimeout, _timeProvider);

                    await using var transaction = await dbContext.Database.BeginTransactionAsync(_isolationLevel, timeoutToken.Token)
                        .ConfigureAwait(false);

                    try
                    {
                        List<OutboxState> outboxStateList = await dbContext.Set<OutboxState>()
                            .FromSqlRaw(_getOutboxIdStatement)
                            .AsTracking()
                            .ToListAsync(timeoutToken.Token).ConfigureAwait(false);
                        var outboxState = outboxStateList.SingleOrDefault();

                        if (outboxState == null)
                            return -1;

                        outboxState.LockId = lockId;

                        dbContext.Update(outboxState);
                        await dbContext.SaveChangesAsync(timeoutToken.Token).ConfigureAwait(false);

                        int continueProcessing;

                        if (outboxState.Delivered.HasValue)
                        {
                            await RemoveOutbox(dbContext, outboxState, cancellationToken).ConfigureAwait(false);

                            // cleanup counts as progress so the outer loop keeps draining delivered outboxes
                            // without falling through to WaitForDelivery; distinct from a zero-progress send fault.
                            continueProcessing = 1;
                        }
                        else
                            continueProcessing = await DeliverOutboxMessages(dbContext, outboxState, cancellationToken).ConfigureAwait(false);

                        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                        return continueProcessing;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        await RollbackTransaction(transaction).ConfigureAwait(false);
                        throw;
                    }
                }

                var executionStrategy = dbContext.Database.CreateExecutionStrategy();

                var messageCount = 0;
                while (messageCount < resultLimit)
                {
                    // Provider-owned execution strategies are the only authoritative source for transient-failure
                    // classification. Always execute through the public strategy contract; inspecting provider error
                    // message text or recognizing only EF's built-in base class would make custom strategies unsafe.
                    var executeResult = await EntityFrameworkExecutionStrategy.ExecuteAsync(
                            dbContext,
                            executionStrategy,
                            Execute,
                            cancellationToken)
                        .ConfigureAwait(false);

                    // executeResult < 0: no outbox found (nothing to do)
                    // executeResult == 0: pending outbox locked but no messages delivered (send fault or poison
                    //   message). Break so the outer loop falls through to WaitForDelivery(QueryDelay), rather than
                    //   re-locking the same outbox immediately and spinning on the same poison message.
                    // executeResult > 0: progress (messages delivered, or delivered-outbox cleanup). Continue.
                    if (executeResult <= 0)
                        break;

                    messageCount += executeResult;
                }

                return messageCount;
            }
            finally
            {
                if (dbContext != null)
                    await dbContext.DisposeAsync().ConfigureAwait(false);

                await scope.DisposeAsync().ConfigureAwait(false);
            }
        }

        static async Task RemoveOutbox(TDbContext dbContext, OutboxState outboxState, CancellationToken cancellationToken)
        {
            List<OutboxMessage> messages = await dbContext.Set<OutboxMessage>()
                .Where(x => x.OutboxId == outboxState.OutboxId)
                .ToListAsync(cancellationToken);

            dbContext.RemoveRange(messages);

            dbContext.Remove(outboxState);

            await dbContext.SaveChangesAsync(cancellationToken);

            if (messages.Count > 0)
                LogContext.Debug?.Log("Outbox removed {Count} messages: {OutboxId}", messages.Count, outboxState);
        }

        async Task<int> DeliverOutboxMessages(TDbContext dbContext, OutboxState outboxState, CancellationToken cancellationToken)
        {
            var messageLimit = _options.MessageDeliveryLimit;

            var hasLastSequenceNumber = outboxState.LastSequenceNumber.HasValue;

            var lastSequenceNumber = outboxState.LastSequenceNumber ?? 0;

            IList<OutboxMessage> messages = await _outboxMessagesQuery(dbContext, outboxState.OutboxId, lastSequenceNumber, messageLimit)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var sentSequenceNumber = 0L;

            var saveChanges = false;

            var messageCount = 0;
            var messageIndex = 0;
            for (; messageIndex < messages.Count && messageCount < messageLimit; messageIndex++)
            {
                var message = messages[messageIndex];

                message.Deserialize(SystemTextJsonMessageSerializer.Instance);

                if (message.DestinationAddress == null)
                {
                    LogContext.Warning?.Log("Outbox message DestinationAddress not present: {SequenceNumber} {MessageId}", message.SequenceNumber,
                        message.MessageId);
                }
                else
                {
                    try
                    {
                        using var sendToken = new CancellationTokenSource(_options.MessageDeliveryTimeout, _timeProvider);
                        using var token = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, sendToken.Token);

                        var pipe = new OutboxMessageSendPipe(message, message.DestinationAddress);

                        var endpoint = await _busControl.GetSendEndpoint(message.DestinationAddress).ConfigureAwait(false);

                        StartedActivity? activity = LogContext.Current?.StartOutboxDeliverActivity(message);
                        var instrument = LogContext.Current?.StartOutboxDeliveryInstrument();

                        try
                        {
                            await endpoint.Send(new SerializedMessageBody(), pipe, token.Token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            activity?.AddExceptionEvent(ex);
                            instrument?.RecordException(ex);
                            throw;
                        }
                        finally
                        {
                            activity?.Stop();
                            instrument?.Complete();
                        }

                        sentSequenceNumber = message.SequenceNumber;

                        LogContext.Debug?.Log("Outbox Sent: {OutboxId} {SequenceNumber} {MessageId}", message.OutboxId, sentSequenceNumber, message.MessageId);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (OperationCanceledException ex)
                    {
                        LogContext.Warning?.Log(ex,
                            "Outbox Send Timeout after {Timeout}: {OutboxId} {SequenceNumber} {MessageId}",
                            _options.MessageDeliveryTimeout, message.OutboxId, message.SequenceNumber, message.MessageId);
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogContext.Warning?.Log(ex, "Outbox Send Fault: {OutboxId} {SequenceNumber} {MessageId}", message.OutboxId, message.SequenceNumber,
                            message.MessageId);

                        break;
                    }

                    dbContext.Remove((object)message);

                    saveChanges = true;

                    messageCount++;
                }
            }

            if (sentSequenceNumber > 0)
            {
                outboxState.LastSequenceNumber = sentSequenceNumber;
                dbContext.Update(outboxState);

                saveChanges = true;
            }

            if (messageIndex == messages.Count && messages.Count < messageLimit)
            {
                outboxState.Delivered = _timeProvider.GetUtcNow().UtcDateTime;

                if (hasLastSequenceNumber == false)
                {
                    dbContext.Remove(outboxState);
                    dbContext.RemoveRange(messages);
                }
                else
                    dbContext.Update(outboxState);

                saveChanges = true;

                LogContext.Debug?.Log("Outbox Delivered: {OutboxId} {Delivered}", outboxState.OutboxId, outboxState.Delivered);
            }

            if (saveChanges)
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return messageCount;
        }

        static async Task RollbackTransaction(IDbContextTransaction transaction)
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                //
            }
        }
    }
}
