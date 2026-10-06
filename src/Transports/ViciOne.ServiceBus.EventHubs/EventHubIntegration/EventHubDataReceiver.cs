using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.EventHubs.Checkpoints;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Runs an Event Hubs processor client and dispatches events through bounded, partition-aware receive execution.</summary>
public class EventHubDataReceiver :
    ConsumerAgent<PartitionOffset>,
    IEventHubDataReceiver
{
    readonly CancellationTokenSource _checkpointTokenSource;
    readonly EventProcessorClient _client;
    readonly ReceiveEndpointContext _context;
    readonly EventHubReceiveAdmission _admission;
    readonly IPartitionedTaskExecutor<ProcessEventArgs> _executorPool;
    readonly IProcessorLockContext _lockContext;

    /// <summary>Starts the processor client and initializes receive admission, partition dispatch, and checkpoint coordination.</summary>
    /// <param name="receiveSettings">The endpoint concurrency, prefetch, and checkpoint settings.</param>
    /// <param name="context">The owning receive-endpoint context.</param>
    /// <param name="processorContext">The active processor context that owns the Azure SDK client.</param>
    public EventHubDataReceiver(ReceiveSettings receiveSettings, ReceiveEndpointContext context, ProcessorContext processorContext)
        : base(context)
    {
        _context = context;
        _checkpointTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Stopped);

        var lockContext = new ProcessorLockContext(processorContext, receiveSettings, _checkpointTokenSource.Token);

        IPartitionHashGenerator hashGenerator = new Murmur3PartitionHashGenerator();
        _executorPool = new PartitionedTaskExecutor<ProcessEventArgs>(GetBytes,
            receiveSettings.ConcurrentMessageLimit,
            receiveSettings.ConcurrentDeliveryLimit,
            hashGenerator: hashGenerator);

        _client = lockContext.Client;
        _lockContext = lockContext;
        _admission = new EventHubReceiveAdmission(receiveSettings.PrefetchCount, _lockContext, _executorPool);

        _client.ProcessErrorAsync += HandleErrorAsync;
        _client.ProcessEventAsync += HandleMessageAsync;

        TrySetManualConsumeTask();

        SetReady(_client.StartProcessingAsync(Stopping));
    }

    async Task HandleErrorAsync(ProcessErrorEventArgs eventArgs)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        if (IsIdle)
        {
            LogContext.Debug?.Log("Receiver shutdown completed: {InputAddress}, PartitionId: {PartitionId}", _context.InputAddress, eventArgs.PartitionId);

            TrySetConsumeException(eventArgs.Exception);
        }
    }

    static byte[] GetBytes(ProcessEventArgs eventArgs)
    {
        var partitionKey = eventArgs.Data.PartitionKey;
        return !string.IsNullOrEmpty(partitionKey) ? Encoding.UTF8.GetBytes(partitionKey) : [];
    }

    async Task HandleMessageAsync(ProcessEventArgs eventArgs)
    {
        if (IsStopping || !eventArgs.HasEvent)
            return;

        await _admission.EnqueueAsync(eventArgs, () => HandleAsync(eventArgs), Stopping).ConfigureAwait(false);
    }

    async Task HandleAsync(ProcessEventArgs eventArgs)
    {
        if (IsStopping)
            return;

        var context = new EventHubReceiveContext(eventArgs, _context);
        var cancellationToken = context.CancellationToken;
        CancellationTokenRegistration? registration = null;
        if (cancellationToken.CanBeCanceled)
            registration = cancellationToken.Register(() => _lockContext.Canceled(eventArgs, cancellationToken));

        try
        {
            await DispatchAsync(eventArgs, context, new EventHubReceiveLockContext(eventArgs, _lockContext)).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            context.LogTransportFaulted(exception);
        }
        finally
        {
            registration?.Dispose();
            context.Dispose();
        }
    }

    /// <summary>Stops the processor, drains partitioned dispatch, ends checkpoint waiting, and releases receive resources.</summary>
    /// <param name="context">The stop context controlling receiver shutdown.</param>
    /// <returns>A task that completes after the processor and checkpoint resources have stopped.</returns>
    protected override async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        Task? stopProcessing = null;
        Exception? stopFailure = null;
        List<Exception>? cleanupFailures = null;

        try
        {
            stopProcessing = _client.StopProcessingAsync();
        }
        catch (Exception exception)
        {
            stopFailure = exception;
        }

        try
        {
            await base.ActiveAndActualAgentsCompletedAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= new List<Exception>()).Add(exception);
        }

        try
        {
            await _executorPool.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= new List<Exception>()).Add(exception);
        }

        try
        {
            _checkpointTokenSource.Cancel();
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= new List<Exception>()).Add(exception);
        }

        if (stopFailure == null)
        {
            try
            {
                await stopProcessing!.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                stopFailure = exception;
            }
        }

        try
        {
            _client.ProcessEventAsync -= HandleMessageAsync;
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= new List<Exception>()).Add(exception);
        }

        try
        {
            _client.ProcessErrorAsync -= HandleErrorAsync;
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= new List<Exception>()).Add(exception);
        }

        try
        {
            _admission.Dispose();
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= new List<Exception>()).Add(exception);
        }

        try
        {
            await _lockContext.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= new List<Exception>()).Add(exception);
        }

        try
        {
            _checkpointTokenSource.Dispose();
        }
        catch (Exception exception)
        {
            (cleanupFailures ??= new List<Exception>()).Add(exception);
        }

        if (stopFailure != null)
            (cleanupFailures ??= new List<Exception>()).Insert(0, stopFailure);

        if (cleanupFailures?.Count == 1)
            ExceptionDispatchInfo.Capture(cleanupFailures[0]).Throw();
        if (cleanupFailures != null)
            throw new AggregateException("Event Hubs receiver shutdown failed in multiple owned stages.", cleanupFailures);
    }
}
