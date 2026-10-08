using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.EventHubs.Checkpoints;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Owns the pending-event queue and batch checkpointer for one Event Hubs partition.</summary>
public class PartitionCheckpointData
{
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly ICheckpointer _checkpointer;
    readonly PendingConfirmationCollection _pending;

    /// <summary>Creates partition checkpoint state using the endpoint's batching settings.</summary>
    /// <param name="settings">The endpoint receive and checkpoint settings.</param>
    /// <param name="pending">The collection shared for tracking unconfirmed events.</param>
    public PartitionCheckpointData(ReceiveSettings settings, PendingConfirmationCollection pending)
        : this(settings, pending, TimeProvider.System)
    {
    }

    internal PartitionCheckpointData(ReceiveSettings settings, PendingConfirmationCollection pending, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _cancellationTokenSource = new CancellationTokenSource();
        _checkpointer = new BatchCheckpointer(settings, _cancellationTokenSource.Token, timeProvider);
        _pending = pending;
    }

    /// <summary>Registers an event as unconfirmed and queues it for ordered checkpoint processing.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="cancellationToken">Cancels registration or queue admission.</param>
    /// <returns>A task that completes when the event is queued.</returns>
    public Task PendingAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        var pendingConfirmation = _pending.Add(eventArgs);
        return _checkpointer.PendingAsync(pendingConfirmation, cancellationToken: cancellationToken);
    }

    /// <summary>Stops this partition's checkpoint worker, draining on shutdown and canceling it for other close reasons.</summary>
    /// <param name="args">The Azure SDK partition-closing arguments.</param>
    /// <param name="cancellationToken">Cancels closure before it begins.</param>
    /// <returns>A task that completes after the checkpoint worker stops.</returns>
    public async Task CloseAsync(PartitionClosingEventArgs args, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (args.Reason != ProcessingStoppedReason.Shutdown)
            _cancellationTokenSource.Cancel();

        await _checkpointer.DisposeAsync().ConfigureAwait(false);

        try
        {
            LogContext.Info?.Log("Partition: {PartitionId} was closed, reason: {Reason}", args.PartitionId, args.Reason);
        }
        catch (global::System.Exception)
        {
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}
