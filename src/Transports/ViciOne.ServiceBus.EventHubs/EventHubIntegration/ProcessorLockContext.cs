using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.EventHubs.Checkpoints;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Coordinates partition lifecycle, pending event confirmations, and batched checkpoints for one processor client.</summary>
public class ProcessorLockContext :
    IProcessorLockContext,
    ProcessorClientBuilderContext
{
    readonly ProcessorContext _context;
    readonly SingleThreadedDictionary<string, PartitionCheckpointData> _data;
    readonly PendingConfirmationCollection _pending;
    readonly ReceiveSettings _receiveSettings;
    readonly TimeProvider _timeProvider;

    /// <summary>Leases the processor client and creates shared pending-confirmation state.</summary>
    /// <param name="context">The processor context that owns the client.</param>
    /// <param name="receiveSettings">The endpoint concurrency and checkpoint settings.</param>
    /// <param name="cancellationToken">Cancels every outstanding confirmation.</param>
    public ProcessorLockContext(ProcessorContext context, ReceiveSettings receiveSettings, CancellationToken cancellationToken)
        : this(context, receiveSettings, cancellationToken, TimeProvider.System)
    {
    }

    internal ProcessorLockContext(ProcessorContext context, ReceiveSettings receiveSettings, CancellationToken cancellationToken, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _context = context;
        _receiveSettings = receiveSettings;
        _pending = new PendingConfirmationCollection(cancellationToken);
        _data = new SingleThreadedDictionary<string, PartitionCheckpointData>(StringComparer.Ordinal);

        Client = context.GetClient(this);
    }

    /// <summary>Gets the leased Azure SDK event processor client.</summary>
    public EventProcessorClient Client { get; }

    /// <summary>Releases the client lease and disposes pending-confirmation cancellation state.</summary>
    /// <returns>A completed value task.</returns>
    public ValueTask DisposeAsync()
    {
        Exception? releaseFailure = null;
        try
        {
            _context.ReleaseClient();
        }
        catch (Exception exception)
        {
            releaseFailure = exception;
        }

        try
        {
            _pending.Dispose();
        }
        catch (Exception pendingFailure) when (releaseFailure != null)
        {
            throw new AggregateException("Processor client release and pending-confirmation cleanup both failed.", releaseFailure, pendingFailure);
        }

        if (releaseFailure != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(releaseFailure).Throw();

        return default;
    }

    /// <summary>Registers an event with its partition's checkpoint queue when that partition is active.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="cancellationToken">Cancels registration or queue admission.</param>
    /// <returns>A task that completes when the event is queued, or immediately when the partition is unknown.</returns>
    public Task PendingAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        return _data.TryGetValue(eventArgs.Partition.PartitionId, out var data) ? data.PendingAsync(eventArgs, cancellationToken: cancellationToken) : Task.CompletedTask;
    }

    /// <summary>Removes and faults the pending confirmation for an event.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="exception">The receive-pipeline failure.</param>
    /// <param name="cancellationToken">Cancels fault reporting before it is applied.</param>
    /// <returns>A completed task after the failure is recorded.</returns>
    public Task FaultedAsync(ProcessEventArgs eventArgs, Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        LogContext.SetCurrentIfNull(_context.LogContext);

        _pending.Faulted(eventArgs, exception);

        return Task.CompletedTask;
    }

    /// <summary>Removes and completes the pending confirmation for an event.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="cancellationToken">Cancels completion reporting before it is applied.</param>
    /// <returns>A completed task after success is recorded.</returns>
    public Task CompleteAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        LogContext.SetCurrentIfNull(_context.LogContext);

        _pending.Complete(eventArgs);

        return Task.CompletedTask;
    }

    /// <summary>Removes and cancels the pending confirmation for an event.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="cancellationToken">The token that caused cancellation.</param>
    public void Canceled(ProcessEventArgs eventArgs, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        _pending.Canceled(eventArgs, cancellationToken);
    }

    /// <summary>Creates checkpoint state before processing begins for a partition.</summary>
    /// <param name="eventArgs">The Azure SDK partition-initializing arguments.</param>
    /// <param name="cancellationToken">Cancels initialization before state is created.</param>
    /// <returns>A completed task after partition state is registered.</returns>
    public Task OnPartitionInitializingAsync(PartitionInitializingEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        LogContext.SetCurrentIfNull(_context.LogContext);

        if (_data.TryAdd(eventArgs.PartitionId, _ => new PartitionCheckpointData(_receiveSettings, _pending, _timeProvider)))
        {
            try
            {
                LogContext.Info?.Log("Partition: {PartitionId} was initialized", eventArgs.PartitionId);
            }
            catch (Exception)
            {
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>Removes and closes checkpoint state after processing stops for a partition.</summary>
    /// <param name="eventArgs">The Azure SDK partition-closing arguments.</param>
    /// <param name="cancellationToken">Cancels partition-state closure.</param>
    /// <returns>A task that completes after partition state is closed, or immediately when no state exists.</returns>
    public Task OnPartitionClosingAsync(PartitionClosingEventArgs eventArgs, CancellationToken cancellationToken = default)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        return _data.TryRemove(eventArgs.PartitionId, out var data) ? data.CloseAsync(eventArgs, cancellationToken: cancellationToken) : Task.CompletedTask;
    }
}
