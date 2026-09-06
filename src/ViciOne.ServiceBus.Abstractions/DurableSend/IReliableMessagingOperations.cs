using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.Operations;

/// <summary>Bounded technical operations for both reliable-messaging quarantines. Authorization, approval, audit and UI remain host-owned.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IReliableMessagingOperations<TBus>
    where TBus : class, IBus
{
    /// <summary>Gets snapshot.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets outbox quarantine.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<DurableSendQuarantinePage> GetOutboxQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one bounded inbox-quarantine page.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<ReliableInboxQuarantinePage> GetInboxQuarantineAsync(
        ReliableInboxQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the current message to its queue.</summary>
    /// <param name="reference">The typed outbox or inbox reference.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requeue outcome.</returns>
    Task<ReliableMessagingOperationResult> RequeueAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default);

    /// <summary>Discards the current value.</summary>
    /// <param name="reference">The typed outbox or inbox reference.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the discard outcome.</returns>
    Task<ReliableMessagingOperationResult> DiscardAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default);

    /// <summary>Marks an inbox quarantine entry as deliberately abandoned and retains its audit state.</summary>
    /// <param name="reference">The reference.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the result of abandon.</returns>
    Task<ReliableMessagingOperationResult> AbandonAsync(
        ReliableMessageReference reference,
        CancellationToken cancellationToken = default);
}
