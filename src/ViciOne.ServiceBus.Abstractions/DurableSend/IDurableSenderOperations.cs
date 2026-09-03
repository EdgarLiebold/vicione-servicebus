using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// Bounded technical operations for durable sender quarantine. Authorization, approval, audit and UI remain host-owned.
/// </summary>
public interface IDurableSenderOperations<TBus>
    where TBus : class, IBus
{
    Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DurableSendQuarantineEntry>> GetQuarantineAsync(
        int maximumCount,
        CancellationToken cancellationToken = default);

    Task<bool> RequeueAsync(DurableSendId id, CancellationToken cancellationToken = default);

    Task<bool> DiscardAsync(DurableSendId id, CancellationToken cancellationToken = default);
}
