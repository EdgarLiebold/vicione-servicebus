using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.Operations;

/// <summary>
/// Bounded technical operations for both reliable-messaging quarantines. Authorization, approval, audit and UI remain host-owned.
/// </summary>
public interface IReliableMessagingOperations<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Gets snapshot.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outbox quarantine.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<DurableSendQuarantinePage> GetOutboxQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one bounded inbox-quarantine page.</summary>
    Task<ReliableInboxQuarantinePage> GetInboxQuarantineAsync(
        ReliableInboxQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the requeue operation.
    /// </summary>
    /// <param name="reference">The typed outbox or inbox reference.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ReliableMessagingOperationResult> RequeueAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the discard operation.
    /// </summary>
    /// <param name="reference">The typed outbox or inbox reference.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ReliableMessagingOperationResult> DiscardAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default);

    /// <summary>Marks an inbox quarantine entry as deliberately abandoned and retains its audit state.</summary>
    /// <param name="reference">The inbox reference to abandon.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The typed operation outcome.</returns>
    Task<ReliableMessagingOperationResult> AbandonAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default);
}
