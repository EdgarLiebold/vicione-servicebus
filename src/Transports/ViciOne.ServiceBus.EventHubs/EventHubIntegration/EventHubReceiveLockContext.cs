using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub receive lock context implementation.
/// </summary>
public class EventHubReceiveLockContext :
    ReceiveLockContext
{
    readonly ProcessEventArgs _eventArgs;
    readonly IProcessorLockContext _lockContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="lockContext">The lock context value.</param>
    public EventHubReceiveLockContext(ProcessEventArgs eventArgs, IProcessorLockContext lockContext)
    {
        _eventArgs = eventArgs;
        _lockContext = lockContext;
    }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        return _lockContext.CompleteAsync(_eventArgs, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return _lockContext.FaultedAsync(_eventArgs, exception, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Validates lock status.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
