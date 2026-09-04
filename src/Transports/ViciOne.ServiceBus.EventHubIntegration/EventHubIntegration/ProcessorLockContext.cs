using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.EventHubIntegration.Checkpoints;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class ProcessorLockContext :
    IProcessorLockContext,
    ProcessorClientBuilderContext
{
    readonly ProcessorContext _context;
    readonly SingleThreadedDictionary<string, PartitionCheckpointData> _data;
    readonly PendingConfirmationCollection _pending;
    readonly ReceiveSettings _receiveSettings;

    public ProcessorLockContext(ProcessorContext context, ReceiveSettings receiveSettings, CancellationToken cancellationToken)
    {
        _context = context;
        _receiveSettings = receiveSettings;
        _pending = new PendingConfirmationCollection(cancellationToken);
        _data = new SingleThreadedDictionary<string, PartitionCheckpointData>(StringComparer.Ordinal);

        Client = context.GetClient(this);
    }

    public EventProcessorClient Client { get; }

    public ValueTask DisposeAsync()
    {
        _context.ReleaseClient(this);

        _pending.Dispose();

        return default;
    }

    public Task PendingAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        return _data.TryGetValue(eventArgs.Partition.PartitionId, out var data) ? data.PendingAsync(eventArgs, cancellationToken: cancellationToken) : Task.CompletedTask;
    }

    public Task FaultedAsync(ProcessEventArgs eventArgs, Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); LogContext.SetCurrentIfNull(_context.LogContext);

        _pending.Faulted(eventArgs, exception);

        return Task.CompletedTask;
    }

    public Task CompleteAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); LogContext.SetCurrentIfNull(_context.LogContext);

        _pending.Complete(eventArgs);

        return Task.CompletedTask;
    }

    public void Canceled(ProcessEventArgs eventArgs, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        _pending.Canceled(eventArgs, cancellationToken);
    }

    public Task OnPartitionInitializingAsync(PartitionInitializingEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); LogContext.SetCurrentIfNull(_context.LogContext);

        if (_data.TryAdd(eventArgs.PartitionId, _ => new PartitionCheckpointData(_receiveSettings, _pending)))
            LogContext.Info?.Log("Partition: {PartitionId} was initialized", eventArgs.PartitionId);

        return Task.CompletedTask;
    }

    public Task OnPartitionClosingAsync(PartitionClosingEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        return _data.TryRemove(eventArgs.PartitionId, out var data) ? data.CloseAsync(eventArgs, cancellationToken: cancellationToken) : Task.CompletedTask;
    }
}
