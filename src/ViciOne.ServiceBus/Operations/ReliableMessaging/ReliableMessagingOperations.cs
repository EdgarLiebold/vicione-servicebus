using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Monitoring.Telemetry;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.Operations.ReliableMessaging;

internal sealed class ReliableMessagingOperations<TBus> : IReliableMessagingOperations<TBus>
    where TBus : class, IBus
{
    readonly IOutboxStore<TBus> _store;
    readonly IInboxStore<TBus> _inbox;
    readonly ServiceBusInstrumentation<TBus> _instrumentation;
    readonly ILogger<ReliableMessagingOperations<TBus>> _logger;
    readonly TimeProvider _timeProvider;

    public ReliableMessagingOperations(
        IEnumerable<IOutboxStore<TBus>> stores,
        IEnumerable<IInboxStore<TBus>> inboxStores,
        TimeProvider timeProvider,
        ILogger<ReliableMessagingOperations<TBus>> logger,
        ServiceBusInstrumentation<TBus> instrumentation)
    {
        _store = ReliableMessagingComposition.RequireExactlyOne<IOutboxStore<TBus>, TBus>(stores, "persistence store");
        _inbox = ReliableMessagingComposition.RequireExactlyOne<IInboxStore<TBus>, TBus>(inboxStores, "inbox store");
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
    }

    public Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        => _store.GetSnapshotAsync(cancellationToken);

    public async Task<DurableSendQuarantinePage> GetOutboxQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = DurableSendQuarantinePagination.Validate(query);
        DurableSendQuarantinePage page = await _store.GetQuarantineAsync(query, cancellationToken).ConfigureAwait(false);
        return ReliableMessagingProviderGuard.ValidateOutboxPage(query, page);
    }

    public async Task<ReliableInboxQuarantinePage> GetInboxQuarantineAsync(
        ReliableInboxQuarantineQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = ReliableInboxQuarantinePagination.Validate(query);
        ReliableInboxQuarantinePage page = await _inbox.GetQuarantineAsync(query, cancellationToken).ConfigureAwait(false);
        return ReliableMessagingProviderGuard.ValidateInboxPage(query, page);
    }

    public async Task<ReliableMessagingOperationResult> RequeueAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default)
    {
        reference = reference.Validate();

        if (reference.Kind == ReliableMessageKind.Inbox)
        {
            ReliableMessagingOperationResult inboxResult = await _inbox
                .RequeueAsync(reference.InboxKey, _timeProvider.GetUtcNow(), cancellationToken)
                .ConfigureAwait(false);
            return ReliableMessagingProviderGuard.ValidateMessagingOperation(reference, inboxResult);
        }

        DurableSendOperationResult result = await _store
            .RequeueAsync(reference.OutboxId, _timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
        result = ReliableMessagingProviderGuard.ValidateOutboxOperation(reference.OutboxId, result);
        return Map(reference, result, "RetryScheduled");
    }

    public async Task<ReliableMessagingOperationResult> DiscardAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default)
    {
        reference = reference.Validate();

        if (reference.Kind == ReliableMessageKind.Inbox)
        {
            ReliableMessagingOperationResult inboxResult = await _inbox
                .DiscardAsync(reference.InboxKey, cancellationToken)
                .ConfigureAwait(false);
            return ReliableMessagingProviderGuard.ValidateMessagingOperation(reference, inboxResult);
        }

        DurableSendOperationResult result = await _store
            .DiscardQuarantinedAsync(reference.OutboxId, cancellationToken)
            .ConfigureAwait(false);
        result = ReliableMessagingProviderGuard.ValidateOutboxOperation(reference.OutboxId, result);
        return Map(reference, result, "Discarded");
    }

    public async Task<ReliableMessagingOperationResult> AbandonAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default)
    {
        reference = reference.Validate();

        if (reference.Kind != ReliableMessageKind.Inbox)
        {
            return new ReliableMessagingOperationResult(
                reference,
                ReliableMessagingOperationDisposition.InvalidState,
                "Quarantined",
                "Quarantined");
        }

        ReliableMessagingOperationResult result = await _inbox
            .AbandonAsync(reference.InboxKey, _timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
        result = ReliableMessagingProviderGuard.ValidateMessagingOperation(reference, result);
        if (result.Disposition == ReliableMessagingOperationDisposition.Applied)
        {
            _instrumentation.RecordReliabilityAbandoned(reference.Kind);
            try
            {
                _logger.LogWarning(
                    "Reliable inbox record abandoned: {BusType} {MessageId} {ConsumerId}",
                    typeof(TBus).FullName,
                    reference.InboxKey.MessageId,
                    reference.InboxKey.ConsumerId);
            }
            catch
            {
                // The retained operator decision is authoritative; logging is observation-only.
            }
        }

        return result;
    }

    static ReliableMessagingOperationResult Map(
        ReliableMessageReference reference,
        DurableSendOperationResult result,
        string appliedState) => result.Outcome switch
        {
            DurableSendOperationOutcome.Requeued or DurableSendOperationOutcome.Discarded => new(
                reference,
                ReliableMessagingOperationDisposition.Applied,
                "Quarantined",
                appliedState),
            DurableSendOperationOutcome.NotFound => new(
                reference,
                ReliableMessagingOperationDisposition.NotFound,
                null,
                null),
            _ => new(
                reference,
                ReliableMessagingOperationDisposition.InvalidState,
                "NotQuarantined",
                "NotQuarantined"),
        };
}
