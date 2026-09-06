using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Tracks receive-pipeline completion and checkpoint state for events processed by an Event Hubs client.</summary>
public interface IProcessorLockContext :
    IAsyncDisposable
{
    /// <summary>Registers an event as pending for its partition checkpoint.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="cancellationToken">Cancels pending-event registration.</param>
    /// <returns>A task that completes after the event is registered.</returns>
    Task PendingAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default);
    /// <summary>Marks the matching event's receive pipeline as successfully completed.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="cancellationToken">Cancels completion reporting before it is applied.</param>
    /// <returns>A task that completes after success is reported.</returns>
    Task CompleteAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default);
    /// <summary>Marks the matching event's receive pipeline as failed.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="exception">The receive-pipeline failure.</param>
    /// <param name="cancellationToken">Cancels fault reporting before it is applied.</param>
    /// <returns>A task that completes after the failure is reported.</returns>
    Task FaultedAsync(ProcessEventArgs eventArgs, Exception exception, CancellationToken cancellationToken = default);
    /// <summary>Marks the matching event's receive pipeline as canceled.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="cancellationToken">The token that caused cancellation.</param>
    void Canceled(ProcessEventArgs eventArgs, CancellationToken cancellationToken);
}
