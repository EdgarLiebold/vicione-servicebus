using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a receive pipe dispatcher implementation.
/// </summary>
public class ReceivePipeDispatcher :
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="receivePipe">The receive pipe value.</param>
    /// <param name="observers">The observers value.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="inputAddress">The input address value.</param>
    public ReceivePipeDispatcher(IReceivePipe receivePipe, ReceiveObservable observers, IHostConfiguration hostConfiguration, Uri inputAddress)
    {
        _receivePipe = receivePipe;
        _observers = observers;
        _hostConfiguration = hostConfiguration;

        _inputAddress = inputAddress.ToString();
        _activityName = $"{inputAddress.GetDiagnosticEndpointName()} receive";
        _endpointName = inputAddress.GetEndpointName() ?? "";
    }

    /// <summary>
    /// Gets the active dispatch count value.
    /// </summary>
    public int ActiveDispatchCount => _activeDispatchCount;
    /// <summary>
    /// Gets the dispatch count value.
    /// </summary>
    public long DispatchCount => _dispatchCount;
    /// <summary>
    /// Gets the max concurrent dispatch count value.
    /// </summary>
    public int MaxConcurrentDispatchCount => _maxConcurrentDispatchCount;

    /// <summary>
    /// Gets metrics.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public DeliveryMetrics GetMetrics()
    {
        return new Metrics(_dispatchCount, _maxConcurrentDispatchCount);
    }

    /// <summary>
    /// Occurs when zero activity.
    /// </summary>
    public event ZeroActiveDispatchHandler? ZeroActivity;

    /// <summary>
    /// Performs the dispatch operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="receiveLock">The receive lock value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DispatchAsync(ReceiveContext context, ReceiveLockContext receiveLock, CancellationToken cancellationToken = default)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.ReceiveLogContext);

        var active = StartDispatch();

        StartedActivity? activity = LogContext.Current?.StartReceiveActivity(_activityName, _inputAddress, _endpointName, context);
        var instrument = LogContext.Current?.StartReceiveInstrument(context);

        try
        {
            if (_observers.Count > 0)
                await _observers.PreReceiveAsync(context).ConfigureAwait(false);

            var validateLockStatusTask = receiveLock.ValidateLockStatusAsync(cancellationToken: cancellationToken);
            if (validateLockStatusTask.Status != TaskStatus.RanToCompletion)
                await validateLockStatusTask.ConfigureAwait(false);

            await _receivePipe.SendAsync(context).ConfigureAwait(false);

            await context.ReceiveCompleted.ConfigureAwait(false);

            var receiveLockCompleteTask = receiveLock.CompleteAsync(cancellationToken: cancellationToken);
            if (receiveLockCompleteTask.Status != TaskStatus.RanToCompletion)
                await receiveLockCompleteTask.ConfigureAwait(false);

            if (_observers.Count > 0)
                await _observers.PostReceiveAsync(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (_observers.Count > 0)
                await _observers.ReceiveFaultAsync(context, ex).ConfigureAwait(false);

            if (receiveLock != null)
            {
                try
                {
                    var receiveLockFaultedTask = receiveLock.FaultedAsync(ex);
                    if (receiveLockFaultedTask.Status != TaskStatus.RanToCompletion)
                        await receiveLockFaultedTask.ConfigureAwait(false);

                    activity?.AddExceptionEvent(ex);
                    instrument?.RecordException(ex);
                }
                catch (Exception releaseLockException)
                {
                    var aggregateException = new AggregateException("ReceiveLock.Faulted threw an exception", releaseLockException, ex);

                    activity?.AddExceptionEvent(aggregateException);
                    instrument?.RecordException(aggregateException);

                    throw aggregateException;
                }
            }
            else
            {
                activity?.AddExceptionEvent(ex);
                instrument?.RecordException(ex);
            }

            throw;
        }
        finally
        {
            activity?.Stop();
            instrument?.Complete();

            await active.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Connects receive observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _receivePipe.Probe(context);
    }

    /// <summary>
    /// Connects consume pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _receivePipe.ConnectConsumePipe(pipe);
    }

    /// <summary>
    /// Connects consume pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _receivePipe.ConnectConsumePipe(pipe, options);
    }

    /// <summary>
    /// Connects request pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="requestId">The request id value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _receivePipe.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>
    /// Connects consume observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _receivePipe.ConnectConsumeObserver(observer);
    }

    /// <summary>
    /// Connects consume message observer.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return _receivePipe.ConnectConsumeMessageObserver(observer);
    }

    ActiveDispatch StartDispatch()
    {
        var current = Interlocked.Increment(ref _activeDispatchCount);
        while (current > _maxConcurrentDispatchCount)
            Interlocked.CompareExchange(ref _maxConcurrentDispatchCount, current, _maxConcurrentDispatchCount);

        return new ActiveDispatch(Interlocked.Increment(ref _dispatchCount), DispatchCompleteAsync);
    }

    async Task DispatchCompleteAsync(long id)
    {
        var pendingCount = Interlocked.Decrement(ref _activeDispatchCount);
        if (pendingCount == 0)
        {
            var zeroActivity = ZeroActivity;
            if (zeroActivity != null)
            {
                foreach (var @delegate in zeroActivity.GetInvocationList())
                {
                    if (@delegate is ZeroActiveDispatchHandler handler)
                        await handler().ConfigureAwait(false);
                }
            }
        }
    }


    readonly struct ActiveDispatch
    {
        readonly long _id;
        readonly Func<long, Task> _complete;

        public ActiveDispatch(long id, Func<long, Task> complete)
        {
            _id = id;
            _complete = complete;
        }

        public Task CompleteAsync()
        {
            return _complete(_id);
        }
    }


    class Metrics :
        DeliveryMetrics
    {
        public Metrics(long deliveryCount, int concurrentDeliveryCount)
        {
            DeliveryCount = deliveryCount;
            ConcurrentDeliveryCount = concurrentDeliveryCount;
        }

        public long DeliveryCount { get; }
        public int ConcurrentDeliveryCount { get; }
    }
}
