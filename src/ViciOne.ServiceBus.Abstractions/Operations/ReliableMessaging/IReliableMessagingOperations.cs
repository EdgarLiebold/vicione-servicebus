using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Operations;

/// <summary>Bounded technical operations for both reliable-messaging quarantines. Authorization, approval, audit and UI remain host-owned.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IReliableMessagingOperations<TBus>
    where TBus : class, IBus
{
    /// <summary>Gets an aggregate snapshot of retained outbox state.</summary>
    /// <param name="cancellationToken">The token used to cancel retrieval.</param>
    /// <returns>A task containing the bounded aggregate snapshot.</returns>
    Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets one bounded page of payload-free outbox quarantine evidence.</summary>
    /// <param name="query">The outbox seek-pagination query.</param>
    /// <param name="cancellationToken">The token used to cancel retrieval.</param>
    /// <returns>A task containing the requested outbox page.</returns>
    Task<DurableSendQuarantinePage> GetOutboxQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one bounded inbox-quarantine page.</summary>
    /// <param name="query">The inbox seek-pagination query.</param>
    /// <param name="cancellationToken">The token used to cancel retrieval.</param>
    /// <returns>A task containing the requested inbox page.</returns>
    Task<ReliableInboxQuarantinePage> GetInboxQuarantineAsync(
        ReliableInboxQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a quarantined outbox intent or inbox record to its delivery queue.</summary>
    /// <param name="reference">The typed outbox or inbox reference.</param>
    /// <param name="cancellationToken">The token used to cancel the state transition.</param>
    /// <returns>A task containing the exact state-transition result.</returns>
    Task<ReliableMessagingOperationResult> RequeueAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default);

    /// <summary>Permanently removes a quarantined outbox intent or inbox record.</summary>
    /// <param name="reference">The typed outbox or inbox reference.</param>
    /// <param name="cancellationToken">The token used to cancel the state transition.</param>
    /// <returns>A task containing the exact state-transition result.</returns>
    Task<ReliableMessagingOperationResult> DiscardAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default);

    /// <summary>Marks an inbox quarantine entry as deliberately abandoned and retains its audit state.</summary>
    /// <param name="reference">The inbox reference to retain as abandoned evidence.</param>
    /// <param name="cancellationToken">The token used to cancel the state transition.</param>
    /// <returns>A task containing the exact state-transition result.</returns>
    Task<ReliableMessagingOperationResult> AbandonAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default);
}
