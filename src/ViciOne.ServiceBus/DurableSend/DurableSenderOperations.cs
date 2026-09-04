namespace ViciOne.ServiceBus.DurableSend;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.ProviderAbstractions;

internal sealed class DurableSenderOperations<TBus> : IDurableSenderOperations<TBus>
    where TBus : class, IBus
{
    readonly IDurableSendStore<TBus> _store;
    readonly TimeProvider _timeProvider;

    public DurableSenderOperations(IEnumerable<IDurableSendStore<TBus>> stores, TimeProvider timeProvider)
    {
        _store = DurableSenderComposition.RequireExactlyOne<IDurableSendStore<TBus>, TBus>(stores, "persistence store");
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        => _store.GetSnapshotAsync(cancellationToken);

    public Task<DurableSendQuarantinePage> GetQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = DurableSendQuarantinePagination.Validate(query);
        return _store.GetQuarantineAsync(query, cancellationToken);
    }

    public Task<DurableSendOperationResult> RequeueAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default)
        => _store.RequeueAsync(id, _timeProvider.GetUtcNow(), cancellationToken);

    public Task<DurableSendOperationResult> DiscardAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default)
        => _store.DiscardQuarantinedAsync(id, cancellationToken);
}
