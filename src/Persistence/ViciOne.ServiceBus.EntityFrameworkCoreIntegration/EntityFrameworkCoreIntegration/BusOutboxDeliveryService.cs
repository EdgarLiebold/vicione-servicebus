using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.ProviderAbstractions;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

internal sealed class BusOutboxDeliveryService<TBus, TDbContext> :
    BackgroundService
    where TBus : class, IBus
    where TDbContext : DbContext
{
    readonly TBus _bus;
    readonly IBusControl _busControl;
    readonly string _busKey;
    readonly IReadOnlyList<ITransportSendFailureClassifier> _failureClassifiers;
    readonly IsolationLevel _isolationLevel;
    readonly ILockStatementProvider _lockStatementProvider;
    readonly ILogger _logger;
    readonly IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>> _notification;
    readonly OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<TBus, TDbContext>> _options;
    readonly Func<TDbContext, Guid, long, int, IAsyncEnumerable<OutboxMessage>> _outboxMessagesQuery;
    readonly IServiceProvider _provider;
    readonly IRetryPolicy _operationalRetryPolicy;
    readonly TimeProvider _timeProvider;
    string? _getOutboxIdStatement;

    public BusOutboxDeliveryService(IOptions<OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<TBus, TDbContext>>> options,
        IOptions<EntityFrameworkOutboxOptions<TDbContext>> outboxOptions,
        IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>> notification,
        IEnumerable<ITransportSendFailureClassifier> failureClassifiers,
        ILogger<BusOutboxDeliveryService<TBus, TDbContext>> logger,
        IServiceProvider provider,
        TimeProvider timeProvider,
        BusPersistenceIdentity<TBus> persistenceIdentity)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(outboxOptions);
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(failureClassifiers);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(provider);

        _provider = provider;
        _bus = provider.GetRequiredService<TBus>();
        _busControl = ResolveBusControl(provider);
        _busKey = (persistenceIdentity ?? throw new ArgumentNullException(nameof(persistenceIdentity)))
            .Require("Entity Framework bus outbox");
        _notification = notification;
        _logger = logger;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _failureClassifiers = failureClassifiers.ToArray();
        _options = options.Value;
        _lockStatementProvider = outboxOptions.Value.LockStatementProvider;
        _isolationLevel = outboxOptions.Value.IsolationLevel;

        _outboxMessagesQuery = EF.CompileAsyncQuery((TDbContext context, Guid outboxId, long lastSequenceNumber, int limit) =>
            context.Set<OutboxMessage>()
                .Where(x => x.OutboxId == outboxId && x.SequenceNumber > lastSequenceNumber)
                .OrderBy(x => x.SequenceNumber)
                .Take(limit)
                .AsNoTracking());

        _operationalRetryPolicy = Retry.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogContext.ConfigureCurrentLogContextIfNull(_provider);

        using var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = _options.QueryMessageLimit,
            RequestResultLimit = 10
        }, _timeProvider);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _busControl.WaitForHealthStatusAsync(BusHealthStatus.Healthy, stoppingToken).ConfigureAwait(false);

                var count = await _operationalRetryPolicy.RetryAsync(() => algorithm.RunAsync(DeliverOutboxAsync, stoppingToken), stoppingToken)
                    .ConfigureAwait(false);
                if (count > 0)
                    continue;

                await _notification.WaitForDeliveryAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another process won the row. This is normal HA contention and not a delivery failure.
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "EF bus outbox delivery agent faulted for {BusKey}/{DbContext}", _busKey, typeof(TDbContext).Name);
            }
        }
    }

    async Task<int> DeliverOutboxAsync(int resultLimit, CancellationToken cancellationToken)
    {
        await using var scope = _provider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        _getOutboxIdStatement ??= _lockStatementProvider.GetOutboxStatement(dbContext);

        async Task<int> ExecuteAsync()
        {
            using var timeoutToken = new CancellationTokenSource(_options.QueryTimeout, _timeProvider);
            using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken.Token);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(_isolationLevel, linkedToken.Token).ConfigureAwait(false);

            try
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                List<OutboxState> states = await dbContext.Set<OutboxState>()
                    .FromSqlRaw(_getOutboxIdStatement,
                        _busKey,
                        OutboxDeliveryStatus.Pending,
                        OutboxDeliveryStatus.RetryScheduled,
                        now,
                        OutboxDeliveryStatus.Delivered)
                    .AsTracking()
                    .ToListAsync(linkedToken.Token)
                    .ConfigureAwait(false);

                var outboxState = states.SingleOrDefault();
                if (outboxState == null)
                    return -1;

                if (!StringComparer.Ordinal.Equals(outboxState.BusKey, _busKey))
                    throw new InvalidOperationException($"Outbox lock returned a row owned by bus '{outboxState.BusKey}' instead of '{_busKey}'.");

                outboxState.LockId = NewId.NextGuid();
                dbContext.Update(outboxState);
                await dbContext.SaveChangesAsync(linkedToken.Token).ConfigureAwait(false);

                int progress;
                if (outboxState.Status == OutboxDeliveryStatus.Delivered)
                {
                    await RemoveOutboxAsync(dbContext, outboxState, linkedToken.Token).ConfigureAwait(false);
                    progress = 1;
                }
                else
                    progress = await DeliverOutboxMessagesAsync(dbContext, outboxState, linkedToken.Token).ConfigureAwait(false);

                await transaction.CommitAsync(linkedToken.Token).ConfigureAwait(false);
                return progress;
            }
            catch
            {
                await RollbackTransactionAsync(transaction).ConfigureAwait(false);
                throw;
            }
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        var messageCount = 0;
        while (messageCount < resultLimit)
        {
            var executeResult = await EntityFrameworkExecutionStrategy.ExecuteAsync(dbContext, executionStrategy, ExecuteAsync, cancellationToken)
                .ConfigureAwait(false);
            if (executeResult <= 0)
                break;

            messageCount += executeResult;
        }

        return messageCount;
    }

    static async Task RemoveOutboxAsync(TDbContext dbContext, OutboxState outboxState, CancellationToken cancellationToken)
    {
        List<OutboxMessage> messages = await dbContext.Set<OutboxMessage>()
            .Where(x => x.OutboxId == outboxState.OutboxId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        dbContext.RemoveRange(messages);
        dbContext.Remove(outboxState);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("Outbox removed {Count} messages: {OutboxId}", messages.Count, outboxState.OutboxId);
    }

    internal async Task<int> DeliverOutboxMessagesAsync(TDbContext dbContext, OutboxState outboxState, CancellationToken cancellationToken)
    {
        int messageLimit = Math.Max(1, _options.MessageDeliveryLimit);
        bool hasLastSequenceNumber = outboxState.LastSequenceNumber.HasValue;
        long lastSequenceNumber = outboxState.LastSequenceNumber ?? 0;

        IList<OutboxMessage> messages = await _outboxMessagesQuery(dbContext, outboxState.OutboxId, lastSequenceNumber, messageLimit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        long sentSequenceNumber = 0;
        int messageCount = 0;
        int messageIndex = 0;
        bool saveChanges = false;
        bool completedOutbox = false;

        for (; messageIndex < messages.Count && messageCount < messageLimit; messageIndex++)
        {
            var message = messages[messageIndex];
            try
            {
                message.Deserialize(ServiceBusMetadataJson.ObjectDeserializer);
            }
            catch (Exception exception)
            {
                Quarantine(outboxState, message, OutboxFailureKind.InvariantViolation,
                    $"Persisted outbox metadata could not be deserialized: {DescribeFailure(exception)}");
                _logger.LogError(exception,
                    "Outbox message quarantined after persisted metadata failure: {BusKey} {OutboxId} {SequenceNumber}",
                    _busKey, outboxState.OutboxId, message.SequenceNumber);
                saveChanges = true;
                break;
            }

            if (message.DestinationAddress == null)
            {
                Quarantine(outboxState, message, OutboxFailureKind.InvariantViolation,
                    "Persisted outbox message has no DestinationAddress.");
                saveChanges = true;
                break;
            }

            try
            {
                using var sendTimeout = new CancellationTokenSource(_options.MessageDeliveryTimeout, _timeProvider);
                using var sendToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, sendTimeout.Token);

                var pipe = new OutboxMessageSendPipe(message, message.DestinationAddress);
                var endpoint = await _bus.GetSendEndpointAsync(message.DestinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);
                StartedActivity? activity = LogContext.Current?.StartOutboxDeliverActivity(message);
                var instrument = LogContext.Current?.StartOutboxDeliveryInstrument();

                try
                {
                    await endpoint.SendAsync(new SerializedMessageBody(), pipe, sendToken.Token).ConfigureAwait(false);
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
                dbContext.Remove(message);
                outboxState.Status = OutboxDeliveryStatus.Pending;
                ResetFailure(outboxState);
                saveChanges = true;
                messageCount++;

                LogContext.Debug?.Log("Outbox sent: {BusKey} {OutboxId} {SequenceNumber} {MessageId}", _busKey, message.OutboxId,
                    message.SequenceNumber, message.MessageId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                ApplyDeliveryFailure(outboxState, message, exception);
                saveChanges = true;
                break;
            }
        }

        if (sentSequenceNumber > 0)
        {
            outboxState.LastSequenceNumber = sentSequenceNumber;
            dbContext.Update(outboxState);
            saveChanges = true;
        }

        if (outboxState.Status != OutboxDeliveryStatus.Quarantined
            && outboxState.Status != OutboxDeliveryStatus.RetryScheduled
            && messageIndex == messages.Count
            && messages.Count < messageLimit)
        {
            outboxState.Status = OutboxDeliveryStatus.Delivered;
            outboxState.Delivered = _timeProvider.GetUtcNow().UtcDateTime;
            ResetFailure(outboxState);

            if (!hasLastSequenceNumber)
            {
                dbContext.Remove(outboxState);
                dbContext.RemoveRange(messages);
            }
            else
                dbContext.Update(outboxState);

            saveChanges = true;
            completedOutbox = true;
            LogContext.Debug?.Log("Outbox delivered: {BusKey} {OutboxId} {Delivered}", _busKey, outboxState.OutboxId, outboxState.Delivered);
        }

        if (saveChanges)
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return messageCount > 0 || !completedOutbox ? messageCount : 1;
    }

    internal void ApplyDeliveryFailure(OutboxState state, OutboxMessage message, Exception exception)
    {
        var failureKind = Classify(exception);
        if (failureKind == TransportSendFailureKind.Permanent)
        {
            Quarantine(state, message, OutboxFailureKind.Permanent, DescribeFailure(exception));
            _logger.LogError(exception, "Outbox message quarantined after permanent send failure: {BusKey} {OutboxId} {SequenceNumber}",
                _busKey, state.OutboxId, message.SequenceNumber);
            return;
        }

        if (state.DeliveryAttempts < 0 || state.DeliveryAttempts == int.MaxValue)
        {
            Quarantine(state, message, OutboxFailureKind.InvariantViolation,
                $"Persisted DeliveryAttempts value is invalid: {state.DeliveryAttempts}.");
            return;
        }

        int attempt = state.DeliveryAttempts + 1;
        if (attempt >= _options.MaximumDeliveryAttempts)
        {
            state.DeliveryAttempts = attempt;
            Quarantine(state, message, OutboxFailureKind.RetryLimitExceeded, DescribeFailure(exception));
            _logger.LogError(exception, "Outbox message quarantined after {Attempts} attempts: {BusKey} {OutboxId} {SequenceNumber}", attempt,
                _busKey, state.OutboxId, message.SequenceNumber);
            return;
        }

        var delay = CalculateRetryDelay(attempt);
        var now = _timeProvider.GetUtcNow();
        state.Status = OutboxDeliveryStatus.RetryScheduled;
        state.DeliveryAttempts = attempt;
        state.NextDeliveryTime = (now + delay).UtcDateTime;
        state.LastFailureKind = failureKind == TransportSendFailureKind.Transient
            ? OutboxFailureKind.Transient
            : OutboxFailureKind.Unclassified;
        state.LastFailureTime = now.UtcDateTime;
        state.LastFailure = Truncate(exception.ToString(), 2048);
        state.FailedSequenceNumber = message.SequenceNumber;
        state.FailedMessageId = message.MessageId;

        _logger.LogWarning(exception,
            "Outbox send scheduled for retry {Attempt}/{MaximumAttempts} after {Delay}: {BusKey} {OutboxId} {SequenceNumber}",
            attempt, _options.MaximumDeliveryAttempts, delay, _busKey, state.OutboxId, message.SequenceNumber);
    }

    internal TransportSendFailureKind Classify(Exception exception)
    {
        foreach (var classifier in _failureClassifiers)
        {
            try
            {
                if (classifier.TryClassify(exception, out var kind))
                    return kind;
            }
            catch (Exception classifierException)
            {
                _logger.LogWarning(classifierException,
                    "Transport send failure classifier {Classifier} faulted; delivery remains bounded and unclassified.",
                    classifier?.GetType().FullName ?? "<null>");
            }
        }

        return exception switch
        {
            ConnectionException connectionException => connectionException.IsTransient
                ? TransportSendFailureKind.Transient
                : TransportSendFailureKind.Permanent,
            OperationCanceledException => TransportSendFailureKind.Transient,
            TimeoutException => TransportSendFailureKind.Transient,
            ConfigurationException => TransportSendFailureKind.Permanent,
            NotSupportedException => TransportSendFailureKind.Permanent,
            ArgumentException => TransportSendFailureKind.Permanent,
            UnauthorizedAccessException => TransportSendFailureKind.Permanent,
            UriFormatException => TransportSendFailureKind.Permanent,
            _ => TransportSendFailureKind.Unclassified
        };
    }

    void Quarantine(OutboxState state, OutboxMessage message, OutboxFailureKind kind, string reason)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        state.Status = OutboxDeliveryStatus.Quarantined;
        state.NextDeliveryTime = null;
        state.LastFailureKind = kind;
        state.LastFailureTime = now;
        state.LastFailure = Truncate(reason, 2048);
        state.FailedSequenceNumber = message.SequenceNumber;
        state.FailedMessageId = message.MessageId;
    }

    static void ResetFailure(OutboxState state)
    {
        state.NextDeliveryTime = null;
        state.DeliveryAttempts = 0;
        state.LastFailureKind = OutboxFailureKind.None;
        state.LastFailureTime = null;
        state.LastFailure = null;
        state.FailedSequenceNumber = null;
        state.FailedMessageId = null;
    }

    internal TimeSpan CalculateRetryDelay(int attempt)
    {
        long ticks = _options.InitialDeliveryRetryDelay.Ticks;
        long maximumTicks = _options.MaximumDeliveryRetryDelay.Ticks;
        for (var i = 1; i < attempt && ticks < maximumTicks; i++)
            ticks = ticks > maximumTicks / 2 ? maximumTicks : Math.Min(maximumTicks, ticks * 2);

        return TimeSpan.FromTicks(ticks);
    }

    IBusControl ResolveBusControl(IServiceProvider provider)
    {
        if (typeof(TBus) == typeof(IBus))
            return provider.GetRequiredService<IBusControl>();

        return provider.GetRequiredService<IBusInstance<TBus>>().BusControl;
    }

    static string Truncate(string text, int maximumLength)
    {
        return text.Length <= maximumLength ? text : text[..maximumLength];
    }

    static string DescribeFailure(Exception exception)
    {
        try
        {
            return exception.ToString();
        }
        catch
        {
            return exception.GetType().FullName ?? exception.GetType().Name;
        }
    }

    static async Task RollbackTransactionAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Preserve the original failure. Rollback failure is secondary and the transaction is disposed immediately afterwards.
        }
    }
}
