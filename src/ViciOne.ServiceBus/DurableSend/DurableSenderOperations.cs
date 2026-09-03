namespace ViciOne.ServiceBus.DurableSend;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal sealed class DurableSenderOperations<TBus> : IDurableSenderOperations<TBus>
    where TBus : class, IBus
{
    readonly IDurableSendStore<TBus> _store;
    readonly TimeProvider _timeProvider;

    public DurableSenderOperations(IDurableSendStore<TBus> store, TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        => _store.GetSnapshotAsync(cancellationToken);

    public Task<IReadOnlyList<DurableSendQuarantineEntry>> GetQuarantineAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        DurableSendOperationLimits.ValidateQuarantinePageSize(maximumCount, nameof(maximumCount));
        return _store.GetQuarantineAsync(maximumCount, cancellationToken);
    }

    public Task<bool> RequeueAsync(DurableSendId id, CancellationToken cancellationToken = default)
        => _store.RequeueAsync(id, _timeProvider.GetUtcNow(), cancellationToken);

    public Task<bool> DiscardAsync(DurableSendId id, CancellationToken cancellationToken = default)
        => _store.DiscardQuarantinedAsync(id, cancellationToken);
}
