using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transactions;

/// <summary>
/// An explicitly controlled in-memory bus buffer. It is not durable, is not an atomic outbox, and does not flush automatically when
/// its owning dependency-injection scope ends.
/// </summary>
public interface IBufferedBus :
    IBus
{
    /// <summary>
    /// Dispatches the actions that were buffered before this call in FIFO order. An action that was already attempted is never
    /// retried automatically; after a failure or cancellation, every unattempted action remains buffered ahead of actions added
    /// during the flush. Calling this method recursively from an action currently being flushed by the same buffered bus is rejected.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// The call was made recursively from an action currently being flushed by this buffered bus.
    /// </exception>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
