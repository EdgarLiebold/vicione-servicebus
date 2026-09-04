using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.EventHubIntegration.Checkpoints;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class PartitionCheckpointData
{
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly ICheckpointer _checkpointer;
    readonly PendingConfirmationCollection _pending;

    public PartitionCheckpointData(ReceiveSettings settings, PendingConfirmationCollection pending)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _checkpointer = new BatchCheckpointer(settings, _cancellationTokenSource.Token);
        _pending = pending;
    }

    public Task PendingAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        var pendingConfirmation = _pending.Add(eventArgs);
        return _checkpointer.PendingAsync(pendingConfirmation, cancellationToken: cancellationToken);
    }

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
