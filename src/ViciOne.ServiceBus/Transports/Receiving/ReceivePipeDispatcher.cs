using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Dispatches deliveries through receive observers, lock settlement, diagnostics, and the receive pipeline.</summary>
internal sealed class ReceivePipeDispatcher :
    IReceivePipeDispatcher
{
    readonly string _activityName;
    readonly string _endpointName;
    readonly IHostConfiguration _hostConfiguration;
    readonly string _inputAddress;
    readonly ReceiveObservable _observers;
    readonly IReceivePipe _receivePipe;

    int _activeDispatchCount;
    long _dispatchCount;
    int _maxConcurrentDispatchCount;

    /// <summary>Initializes a dispatcher for one endpoint's receive pipeline.</summary>
    /// <param name="receivePipe">The pipeline that processes each delivery.</param>
    /// <param name="observers">The observers notified around pipeline execution.</param>
    /// <param name="hostConfiguration">The host configuration that supplies receive diagnostics.</param>
    /// <param name="inputAddress">The endpoint address reported by diagnostics.</param>
    public ReceivePipeDispatcher(IReceivePipe receivePipe, ReceiveObservable observers, IHostConfiguration hostConfiguration, Uri inputAddress)
    {
        _receivePipe = receivePipe ?? throw new ArgumentNullException(nameof(receivePipe));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        ArgumentNullException.ThrowIfNull(inputAddress);

        _inputAddress = inputAddress.ToString();
        _activityName = $"{inputAddress.GetDiagnosticEndpointName()} receive";
        _endpointName = inputAddress.GetEndpointName() ?? "";
    }

    /// <summary>Gets the number of deliveries currently being dispatched.</summary>
    public int ActiveDispatchCount => Volatile.Read(ref _activeDispatchCount);
    /// <summary>Gets the total number of dispatches started by this instance.</summary>
    public long DispatchCount => Interlocked.Read(ref _dispatchCount);
    /// <summary>Gets the highest number of concurrent dispatches observed by this instance.</summary>
    public int MaxConcurrentDispatchCount => Volatile.Read(ref _maxConcurrentDispatchCount);

    /// <summary>Captures the current cumulative delivery metrics.</summary>
    /// <returns>An immutable metrics snapshot.</returns>
    public IDeliveryMetrics GetMetrics()
    {
        return new Metrics(DispatchCount, MaxConcurrentDispatchCount);
    }

    /// <summary>Occurs after the active dispatch count returns to zero.</summary>
    public event ZeroActivityHandler? ZeroActivity;

    /// <summary>Dispatches one received message and settles its transport lock.</summary>
    /// <param name="context">The received-message context passed to observers and the pipeline.</param>
    /// <param name="receiveLock">The transport lock completed or faulted with the dispatch outcome.</param>
    /// <param name="cancellationToken">The token that cancels lock validation and settlement.</param>
    /// <returns>A task that completes after dispatch, settlement, and activity notification.</returns>
    public async Task DispatchAsync(ReceiveContext context, ReceiveLockContext receiveLock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(receiveLock);
        cancellationToken.ThrowIfCancellationRequested();

        LogContext.SetCurrentIfNull(_hostConfiguration.ReceiveLogContext);

        var active = StartDispatch();

        StartedActivity? activity = MessageActivity.TryStartReceive(_activityName, _inputAddress, _endpointName, context);
        var instrument = LogContext.Current?.TryStartReceiveMetrics(context);

        try
        {
            await DispatchAndSettleAsync(context, receiveLock, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await HandleDispatchFailureAsync(context, receiveLock, exception, activity, instrument).ConfigureAwait(false);

            throw;
        }
        finally
        {
            activity?.Stop();
            instrument?.Complete();

            await active.CompleteAsync().ConfigureAwait(false);
        }
    }

    async Task DispatchAndSettleAsync(
        ReceiveContext context,
        ReceiveLockContext receiveLock,
        CancellationToken cancellationToken)
    {
        if (_observers.Count > 0)
            await _observers.PreReceiveAsync(context).ConfigureAwait(false);

        Task validateLockStatusTask = receiveLock.ValidateLockStatusAsync(cancellationToken)
            ?? throw new InvalidOperationException("The receive lock returned no validation task.");
        if (validateLockStatusTask.Status != TaskStatus.RanToCompletion)
            await validateLockStatusTask.ConfigureAwait(false);

        Task dispatchTask = _receivePipe.SendAsync(context)
            ?? throw new InvalidOperationException("The receive pipe returned no dispatch task.");
        await dispatchTask.ConfigureAwait(false);
        await context.ReceiveCompleted.ConfigureAwait(false);

        Task completionTask = receiveLock.CompleteAsync(cancellationToken)
            ?? throw new InvalidOperationException("The receive lock returned no completion task.");
        if (completionTask.Status != TaskStatus.RanToCompletion)
            await completionTask.ConfigureAwait(false);

        if (_observers.Count > 0)
            await _observers.PostReceiveAsync(context).ConfigureAwait(false);
    }

    async Task HandleDispatchFailureAsync(
        ReceiveContext context,
        ReceiveLockContext receiveLock,
        Exception dispatchFailure,
        StartedActivity? activity,
        MetricOperation? instrument)
    {
        await NotifyReceiveFaultAsync(context, dispatchFailure).ConfigureAwait(false);

        try
        {
            Task settlementTask = receiveLock.FaultedAsync(dispatchFailure)
                ?? throw new InvalidOperationException("The receive lock returned no fault task.");
            if (settlementTask.Status != TaskStatus.RanToCompletion)
                await settlementTask.ConfigureAwait(false);
        }
        catch (Exception settlementFailure)
        {
            var aggregateFailure = new AggregateException(
                "Receive-lock fault settlement failed.",
                dispatchFailure,
                settlementFailure);
            RecordFailure(activity, instrument, aggregateFailure);
            throw aggregateFailure;
        }

        RecordFailure(activity, instrument, dispatchFailure);
    }

    async Task NotifyReceiveFaultAsync(ReceiveContext context, Exception dispatchFailure)
    {
        try
        {
            await context.NotifyFaultedAsync(dispatchFailure).ConfigureAwait(false);
        }
        catch (Exception observerFailure)
        {
            try
            {
                LogContext.Error?.Log(observerFailure,
                    "A receive-fault observer failed after receive dispatch faulted: {InputAddress}", _inputAddress);
            }
            catch
            {
                // Diagnostics cannot replace the dispatch failure or prevent lock settlement.
            }
        }
    }

    static void RecordFailure(StartedActivity? activity, MetricOperation? instrument, Exception failure)
    {
        activity?.AddExceptionEvent(failure);
        instrument?.RecordException(failure);
    }

    /// <summary>Subscribes an observer to receive-pipeline notifications.</summary>
    /// <param name="observer">The observer that receives the notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _observers.Connect(observer);
    }

    /// <summary>Adds receive-pipeline diagnostics to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostics.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _receivePipe.Probe(context);
    }

    /// <summary>Connects a pipeline that receives every consumed message of a contract type.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching messages.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return _receivePipe.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects a configurable pipeline that receives every consumed message of a contract type.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching messages.</param>
    /// <param name="options">The settings that control pipe connection and scheduling.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return _receivePipe.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Connects a pipeline for a specific request and response message contract.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="requestId">The request correlation identifier.</param>
    /// <param name="pipe">The pipeline invoked for matching responses.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return _receivePipe.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Subscribes an observer to consume-pipeline notifications.</summary>
    /// <param name="observer">The observer that receives consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _receivePipe.ConnectConsumeObserver(observer);
    }

    /// <summary>Subscribes a typed observer to consume-pipeline notifications.</summary>
    /// <typeparam name="T">The observed message contract.</typeparam>
    /// <param name="observer">The observer that receives typed consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _receivePipe.ConnectConsumeMessageObserver(observer);
    }

    ActiveDispatch StartDispatch()
    {
        int current = Interlocked.Increment(ref _activeDispatchCount);
        int observed;
        while (current > (observed = Volatile.Read(ref _maxConcurrentDispatchCount))
               && Interlocked.CompareExchange(ref _maxConcurrentDispatchCount, current, observed) != observed)
        {
        }

        Interlocked.Increment(ref _dispatchCount);
        return new ActiveDispatch(DispatchCompleteAsync);
    }

    async Task DispatchCompleteAsync()
    {
        var pendingCount = Interlocked.Decrement(ref _activeDispatchCount);
        if (pendingCount == 0)
        {
            var zeroActivity = ZeroActivity;
            if (zeroActivity != null)
            {
                foreach (var @delegate in zeroActivity.GetInvocationList())
                {
                    if (@delegate is ZeroActivityHandler handler)
                    {
                        try
                        {
                            Task notification = handler()
                                ?? throw new InvalidOperationException("A zero-activity handler returned no task.");
                            await notification.ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            try
                            {
                                LogContext.Error?.Log(exception, "A zero-activity handler failed after receive dispatch completed: {InputAddress}",
                                    _inputAddress);
                            }
                            catch
                            {
                                // Diagnostics cannot change the dispatch outcome or skip later subscribers.
                            }
                        }
                    }
                }
            }
        }
    }


    readonly struct ActiveDispatch
    {
        readonly Func<Task> _complete;

        public ActiveDispatch(Func<Task> complete)
        {
            _complete = complete ?? throw new ArgumentNullException(nameof(complete));
        }

        public Task CompleteAsync()
        {
            return _complete();
        }
    }


    sealed class Metrics :
        IDeliveryMetrics
    {
        public Metrics(long deliveryCount, int concurrentDeliveryCount)
        {
            DeliveryCount = deliveryCount;
            MaxConcurrentDeliveryCount = concurrentDeliveryCount;
        }

        public long DeliveryCount { get; }
        public int MaxConcurrentDeliveryCount { get; }
    }
}
