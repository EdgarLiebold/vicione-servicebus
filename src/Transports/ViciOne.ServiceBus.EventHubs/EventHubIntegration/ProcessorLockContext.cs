using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.EventHubs.Checkpoints;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a processor lock context implementation.
/// </summary>
public class ProcessorLockContext :
    IProcessorLockContext,
    ProcessorClientBuilderContext
{
    readonly ProcessorContext _context;
    readonly SingleThreadedDictionary<string, PartitionCheckpointData> _data;
    readonly PendingConfirmationCollection _pending;
    readonly ReceiveSettings _receiveSettings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="receiveSettings">The receive settings value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ProcessorLockContext(ProcessorContext context, ReceiveSettings receiveSettings, CancellationToken cancellationToken)
    {
        _context = context;
        _receiveSettings = receiveSettings;
        _pending = new PendingConfirmationCollection(cancellationToken);
        _data = new SingleThreadedDictionary<string, PartitionCheckpointData>(StringComparer.Ordinal);

        Client = context.GetClient(this);
    }

    /// <summary>
    /// Gets the client value.
    /// </summary>
    public EventProcessorClient Client { get; }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        _context.ReleaseClient(this);

        _pending.Dispose();

        return default;
    }

    /// <summary>
    /// Performs the pending operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PendingAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        return _data.TryGetValue(eventArgs.Partition.PartitionId, out var data) ? data.PendingAsync(eventArgs, cancellationToken: cancellationToken) : Task.CompletedTask;
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync(ProcessEventArgs eventArgs, Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); LogContext.SetCurrentIfNull(_context.LogContext);

        _pending.Faulted(eventArgs, exception);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompleteAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); LogContext.SetCurrentIfNull(_context.LogContext);

        _pending.Complete(eventArgs);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines whether the current value can celed.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public void Canceled(ProcessEventArgs eventArgs, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        _pending.Canceled(eventArgs, cancellationToken);
    }

    /// <summary>
    /// Performs the on partition initializing operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task OnPartitionInitializingAsync(PartitionInitializingEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); LogContext.SetCurrentIfNull(_context.LogContext);

        if (_data.TryAdd(eventArgs.PartitionId, _ => new PartitionCheckpointData(_receiveSettings, _pending)))
            LogContext.Info?.Log("Partition: {PartitionId} was initialized", eventArgs.PartitionId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the on partition closing operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task OnPartitionClosingAsync(PartitionClosingEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        return _data.TryRemove(eventArgs.PartitionId, out var data) ? data.CloseAsync(eventArgs, cancellationToken: cancellationToken) : Task.CompletedTask;
    }
}
