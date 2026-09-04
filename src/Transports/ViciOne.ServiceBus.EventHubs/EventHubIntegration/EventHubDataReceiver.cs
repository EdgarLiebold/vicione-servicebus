using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.EventHubs.Checkpoints;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub data receiver implementation.
/// </summary>
public class EventHubDataReceiver :
    ConsumerAgent<PartitionOffset>,
    IEventHubDataReceiver
{
    readonly CancellationTokenSource _checkpointTokenSource;
    readonly EventProcessorClient _client;
    readonly ReceiveEndpointContext _context;
    readonly IPartitionedTaskExecutor<ProcessEventArgs> _executorPool;
    readonly SemaphoreSlim _limit;
    readonly IProcessorLockContext _lockContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="receiveSettings">The receive settings value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="processorContext">The processor context value.</param>
    public EventHubDataReceiver(ReceiveSettings receiveSettings, ReceiveEndpointContext context, ProcessorContext processorContext)
        : base(context)
    {
        _context = context;
        _checkpointTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Stopped);
        _limit = new SemaphoreSlim(receiveSettings.PrefetchCount);

        var lockContext = new ProcessorLockContext(processorContext, receiveSettings, _checkpointTokenSource.Token);

        IHashGenerator hashGenerator = new Murmur3UnsafeHashGenerator();
        _executorPool = new PartitionedTaskExecutor<ProcessEventArgs>(GetBytes, hashGenerator,
            receiveSettings.ConcurrentMessageLimit,
            receiveSettings.ConcurrentDeliveryLimit);

        _client = lockContext.Client;
        _lockContext = lockContext;

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

        await _limit.WaitAsync(Stopping).ConfigureAwait(false);
        await _lockContext.PendingAsync(eventArgs).ConfigureAwait(false);
        await _executorPool.EnqueueAsync(eventArgs, () => HandleAsync(eventArgs), Stopping).ConfigureAwait(false);
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
            _limit.Release();
        }
    }

    /// <summary>
    /// Performs the active and actual agents completed operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected override async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        var stopProcessing = _client.StopProcessingAsync();

        await base.ActiveAndActualAgentsCompletedAsync(context).ConfigureAwait(false);

        await _executorPool.DisposeAsync().ConfigureAwait(false);

        // There is not point to wait any longer, we drained our queue
        _checkpointTokenSource.Cancel();

        await stopProcessing.ConfigureAwait(false);
        _client.ProcessEventAsync -= HandleMessageAsync;
        _client.ProcessErrorAsync -= HandleErrorAsync;

        await _lockContext.DisposeAsync().ConfigureAwait(false);
        _checkpointTokenSource.Dispose();
        _limit.Dispose();
    }
}
