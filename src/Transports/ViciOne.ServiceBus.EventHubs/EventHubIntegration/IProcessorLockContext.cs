using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for processor lock context.
/// </summary>
public interface IProcessorLockContext :
    IAsyncDisposable
{
    /// <summary>
    /// Performs the pending operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PendingAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task CompleteAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task FaultedAsync(ProcessEventArgs eventArgs, Exception exception, CancellationToken cancellationToken = default);
    /// <summary>
    /// Determines whether the current value can celed.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    void Canceled(ProcessEventArgs eventArgs, CancellationToken cancellationToken);
}
