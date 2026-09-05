using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Diagnostics;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.Operations;

internal sealed class ReliableMessagingOperations<TBus> : IReliableMessagingOperations<TBus>
    where TBus : class, IBus
{
    readonly IOutboxStore<TBus> _store;
    readonly IInboxStore<TBus> _inbox;
    readonly V5ServiceBusInstrumentation<TBus> _instrumentation;
    readonly ILogger<ReliableMessagingOperations<TBus>> _logger;
    readonly TimeProvider _timeProvider;

    public ReliableMessagingOperations(
        IEnumerable<IOutboxStore<TBus>> stores,
        IEnumerable<IInboxStore<TBus>> inboxStores,
        TimeProvider timeProvider,
        ILogger<ReliableMessagingOperations<TBus>> logger,
        V5ServiceBusInstrumentation<TBus> instrumentation)
    {
        _store = ReliableMessagingComposition.RequireExactlyOne<IOutboxStore<TBus>, TBus>(stores, "persistence store");
        _inbox = ReliableMessagingComposition.RequireExactlyOne<IInboxStore<TBus>, TBus>(inboxStores, "inbox store");
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
    }

    public Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        => _store.GetSnapshotAsync(cancellationToken);

    public Task<DurableSendQuarantinePage> GetOutboxQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = DurableSendQuarantinePagination.Validate(query);
        return _store.GetQuarantineAsync(query, cancellationToken);
    }

    public Task<ReliableInboxQuarantinePage> GetInboxQuarantineAsync(
        ReliableInboxQuarantineQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = ReliableInboxQuarantinePagination.Validate(query);
        return _inbox.GetQuarantineAsync(query, cancellationToken);
    }

    public async Task<ReliableMessagingOperationResult> RequeueAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default)
    {
        if (reference.Kind == ReliableMessageKind.Inbox)
            return await _inbox.RequeueAsync(reference.InboxKey, _timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);

        DurableSendOperationResult result = await _store
            .RequeueAsync(reference.OutboxId, _timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
        return Map(reference, result, "RetryScheduled");
    }

    public async Task<ReliableMessagingOperationResult> DiscardAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default)
    {
        if (reference.Kind == ReliableMessageKind.Inbox)
            return await _inbox.DiscardAsync(reference.InboxKey, cancellationToken).ConfigureAwait(false);

        DurableSendOperationResult result = await _store
            .DiscardQuarantinedAsync(reference.OutboxId, cancellationToken)
            .ConfigureAwait(false);
        return Map(reference, result, "Discarded");
    }

    public async Task<ReliableMessagingOperationResult> AbandonAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default)
    {
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
