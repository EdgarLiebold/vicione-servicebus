using System;
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

    Task<DurableSendQuarantinePage> GetQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default);

    Task<DurableSendOperationResult> RequeueAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default);

    Task<DurableSendOperationResult> DiscardAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default);
}
