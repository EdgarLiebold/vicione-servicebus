using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>Queues event confirmations for durable partition checkpoint updates.</summary>
public interface ICheckpointer :
    IAsyncDisposable
{
    /// <summary>Queues an event confirmation for checkpoint processing.</summary>
    /// <param name="confirmation">The confirmation associated with a received event.</param>
    /// <param name="cancellationToken">Cancels waiting to enqueue the confirmation.</param>
    /// <returns>A task that completes when the confirmation has been accepted.</returns>
    Task PendingAsync(IPendingConfirmation confirmation, CancellationToken cancellationToken = default);
}
