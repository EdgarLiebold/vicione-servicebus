using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.EventHubs.Checkpoints;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a partition checkpoint data implementation.
/// </summary>
public class PartitionCheckpointData
{
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly ICheckpointer _checkpointer;
    readonly PendingConfirmationCollection _pending;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="pending">The pending value.</param>
    public PartitionCheckpointData(ReceiveSettings settings, PendingConfirmationCollection pending)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _checkpointer = new BatchCheckpointer(settings, _cancellationTokenSource.Token);
        _pending = pending;
    }

    /// <summary>
    /// Performs the pending operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PendingAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        var pendingConfirmation = _pending.Add(eventArgs);
        return _checkpointer.PendingAsync(pendingConfirmation, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the close operation.
    /// </summary>
    /// <param name="args">The args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task CloseAsync(PartitionClosingEventArgs args, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (args.Reason != ProcessingStoppedReason.Shutdown)
            _cancellationTokenSource.Cancel();

        await _checkpointer.DisposeAsync().ConfigureAwait(false);

        LogContext.Info?.Log("Partition: {PartitionId} was closed, reason: {Reason}", args.PartitionId, args.Reason);

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}
