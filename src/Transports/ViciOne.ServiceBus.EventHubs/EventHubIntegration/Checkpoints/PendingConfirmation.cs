using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>Tracks receive-pipeline completion and checkpoint state for one Event Hubs event.</summary>
public class PendingConfirmation :
    IPendingConfirmation
{
    readonly TaskCompletionSource<string> _source;
    ProcessEventArgs _eventArgs;

    /// <summary>Creates a pending confirmation for the supplied event.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    public PendingConfirmation(ProcessEventArgs eventArgs)
    {
        _eventArgs = eventArgs;
        _source = TaskCompletionSources.Create<string>();
    }

    Uri Topic => new Uri($"topic:{Partition.EventHubName}");

    /// <summary>Gets the partition that supplied the event.</summary>
    public PartitionContext Partition => _eventArgs.Partition;

    /// <summary>Gets the event's provider-defined offset.</summary>
    public string OffsetString => _eventArgs.Data.OffsetString;

    /// <summary>Gets the task that represents receive-pipeline completion.</summary>
    public Task Confirmed => _source.Task;

    /// <summary>Marks the receive pipeline as successfully completed.</summary>
    public void Complete()
    {
        _source.TrySetResult(OffsetString);
    }

    /// <summary>Marks message consumption as failed.</summary>
    /// <param name="exception">The receive-pipeline failure.</param>
    public void Faulted(Exception exception)
    {
        _source.TrySetException(new MessageNotConsumedException(Topic, "Message not consumed", exception));
    }

    /// <summary>Marks the confirmation as failed with an argument error.</summary>
    /// <param name="message">The error description.</param>
    public void Faulted(string message)
    {
        _source.TrySetException(new ArgumentException(message));
    }

    /// <summary>Marks message consumption as canceled.</summary>
    /// <param name="cancellationToken">The token that caused cancellation.</param>
    public void Canceled(CancellationToken cancellationToken)
    {
        _source.TrySetCanceled(cancellationToken);
    }

    /// <summary>Updates the Event Hubs checkpoint through this event.</summary>
    /// <param name="cancellationToken">Cancels the checkpoint update.</param>
    /// <returns>The Azure SDK operation that advances the partition checkpoint through this event.</returns>
    public Task CheckpointAsync(CancellationToken cancellationToken)
    {
        return _eventArgs.UpdateCheckpointAsync(cancellationToken);
    }
}
