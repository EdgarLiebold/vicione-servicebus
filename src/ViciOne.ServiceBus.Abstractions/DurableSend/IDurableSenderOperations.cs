using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Operations;

/// <summary>
/// Bounded technical operations for durable sender quarantine. Authorization, approval, audit and UI remain host-owned.
/// </summary>
public interface IDurableSenderOperations<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Gets snapshot.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets quarantine.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<DurableSendQuarantinePage> GetQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the requeue operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<DurableSendOperationResult> RequeueAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the discard operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<DurableSendOperationResult> DiscardAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default);
}
