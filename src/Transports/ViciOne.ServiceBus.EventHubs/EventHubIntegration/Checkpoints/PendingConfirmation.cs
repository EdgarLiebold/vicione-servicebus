using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>
/// Provides a pending confirmation implementation.
/// </summary>
public class PendingConfirmation :
    IPendingConfirmation
{
    readonly TaskCompletionSource<string> _source;
    ProcessEventArgs _eventArgs;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    public PendingConfirmation(ProcessEventArgs eventArgs)
    {
        _eventArgs = eventArgs;
        _source = TaskCompletionSources.Create<string>();
    }

    Uri Topic => new Uri($"topic:{Partition.EventHubName}");

    /// <summary>
    /// Gets the partition value.
    /// </summary>
    public PartitionContext Partition => _eventArgs.Partition;

    /// <summary>
    /// Gets the offset string value.
    /// </summary>
    public string OffsetString => _eventArgs.Data.OffsetString;

    /// <summary>
    /// Gets the confirmed value.
    /// </summary>
    public Task Confirmed => _source.Task;

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    public void Complete()
    {
        _source.TrySetResult(OffsetString);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Faulted(Exception exception)
    {
        _source.TrySetException(new MessageNotConsumedException(Topic, "Message not consumed", exception));
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    public void Faulted(string message)
    {
        _source.TrySetException(new ArgumentException(message));
    }

    /// <summary>
    /// Determines whether the current value can celed.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public void Canceled(CancellationToken cancellationToken)
    {
        _source.TrySetCanceled(cancellationToken);
    }

    /// <summary>
    /// Performs the checkpoint operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CheckpointAsync(CancellationToken cancellationToken)
    {
        return _eventArgs.UpdateCheckpointAsync(cancellationToken);
    }
}
