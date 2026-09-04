using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>
/// Defines the contract for checkpointer.
/// </summary>
public interface ICheckpointer :
    IAsyncDisposable
{
    /// <summary>
    /// Performs the pending operation.
    /// </summary>
    /// <param name="confirmation">The confirmation value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PendingAsync(IPendingConfirmation confirmation, CancellationToken cancellationToken = default);
}
