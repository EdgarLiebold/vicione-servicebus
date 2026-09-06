using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Adapts Event Hubs completion tracking to the transport receive-lock contract.</summary>
public class EventHubReceiveLockContext :
    ReceiveLockContext
{
    readonly ProcessEventArgs _eventArgs;
    readonly IProcessorLockContext _lockContext;

    /// <summary>Creates a receive-lock adapter for one processed event.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="lockContext">The processor context that tracks completion and checkpoints.</param>
    public EventHubReceiveLockContext(ProcessEventArgs eventArgs, IProcessorLockContext lockContext)
    {
        _eventArgs = eventArgs;
        _lockContext = lockContext;
    }

    /// <summary>Marks this event's receive pipeline as successfully completed.</summary>
    /// <param name="cancellationToken">Cancels completion reporting before it is applied.</param>
    /// <returns>A task that completes after success is reported.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        return _lockContext.CompleteAsync(_eventArgs, cancellationToken: cancellationToken);
    }

    /// <summary>Marks this event's receive pipeline as failed.</summary>
    /// <param name="exception">The receive-pipeline failure.</param>
    /// <param name="cancellationToken">Cancels fault reporting before it is applied.</param>
    /// <returns>A task that completes after the failure is reported.</returns>
    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return _lockContext.FaultedAsync(_eventArgs, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Completes immediately because Event Hubs does not expose a renewable per-event lock.</summary>
    /// <param name="cancellationToken">Cancels the validation call.</param>
    /// <returns>A completed task unless cancellation was already requested.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
