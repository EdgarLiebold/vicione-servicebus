using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Executes the current routing-slip activity and advances or faults the routing slip.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityHost<TActivity, TArguments> :
    IFilter<ConsumeContext<IRoutingSlip>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly Uri? _compensateAddress;
    readonly IPipe<ExecuteContext<TArguments>> _executePipe;

    /// <summary>Creates a host for the activity execution pipeline and its optional compensation destination.</summary>
    /// <param name="executePipe">The pipeline that deserializes arguments, resolves the activity, and records its result.</param>
    /// <param name="compensateAddress">The endpoint that compensates successful executions, when the activity supports compensation.</param>
    public ExecuteActivityHost(IPipe<ExecuteContext<TArguments>> executePipe, Uri? compensateAddress)
    {
        ArgumentNullException.ThrowIfNull(executePipe);

        _executePipe = executePipe;
        _compensateAddress = compensateAddress;
    }

    /// <summary>Executes the current itinerary entry, evaluates its result, and advances the receive pipeline.</summary>
    /// <param name="context">The received routing slip whose first itinerary entry is executed.</param>
    /// <param name="next">The receive pipeline invoked after the execution outcome is dispatched.</param>
    /// <returns>A task that completes after result dispatch and receive-pipeline continuation.</returns>
    public async Task SendAsync(ConsumeContext<IRoutingSlip> context, IPipe<ConsumeContext<IRoutingSlip>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        TimeProvider timeProvider = context.GetTimeProvider();
        long startedAt = timeProvider.GetTimestamp();

        StartedActivity? activity = CourierActivity.TryStartExecution<TActivity, TArguments>(context);
        var instrument = LogContext.Current?.TryStartExecutionMetrics<TActivity, TArguments>(context);

        try
        {
            await ExecuteCurrentActivityAsync(context, activity, instrument).ConfigureAwait(false);

            await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TActivity>.ShortName).ConfigureAwait(false);

            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception) when (!context.CancellationToken.IsCancellationRequested
                                          && ConsumerIngressFailure.IsCancellation(exception))
        {
            var cancellation = new ConsumerCanceledException(
                $"The operation was canceled by the activity: {TypeCache<TActivity>.ShortName}", exception);

            activity?.AddExceptionEvent(exception);

            instrument?.RecordException(exception);

            await ConsumerIngressFailure.NotifyFaultedAsync(context, timeProvider.GetElapsedTime(startedAt),
                TypeCache<TActivity>.ShortName, cancellation, cancellation).ConfigureAwait(false);

            throw cancellation;
        }
        catch (Exception exception)
        {
            activity?.AddExceptionEvent(exception);

            instrument?.RecordException(exception);

            await ConsumerIngressFailure.NotifyFaultedAsync(context, timeProvider.GetElapsedTime(startedAt),
                TypeCache<TActivity>.ShortName, exception, exception).ConfigureAwait(false);

            throw;
        }
        finally
        {
            activity?.Stop();
            instrument?.Complete();
        }
    }

    async Task ExecuteCurrentActivityAsync(
        ConsumeContext<IRoutingSlip> context,
        StartedActivity? activity,
        MetricOperation? instrument)
    {
        ExecuteContext<TArguments> executeContext = new HostExecuteContext<TArguments>(_compensateAddress, context);
        ExecutionResult result;

        LogContext.Debug?.Log("Execute Activity: {TrackingNumber} ({Activity}, {Host})", executeContext.TrackingNumber,
            TypeCache<TActivity>.ShortName, context.Advanced().ReceiveContext.InputAddress);

        try
        {
            await _executePipe.SendAsync(executeContext).ConfigureAwait(false);

            result = executeContext.Result
                ?? executeContext.Faulted(new ActivityExecutionException("The activity execute did not return a result"));
        }
        catch (Exception exception) when (!IsCancellation(exception))
        {
            ExecutionResult? recordedResult = executeContext.Result;
            if (recordedResult == null || !recordedResult.IsFaulted(out var faultException) || faultException != exception)
                result = executeContext.Result = executeContext.Faulted(exception);
            else
                result = recordedResult;

            activity?.AddExceptionEvent(exception);
            instrument?.RecordException(exception);
        }

        await result.EvaluateAsync(context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds the hosted execution pipeline and activity contract to the probe graph.</summary>
    /// <param name="context">The probe context that receives the execution filter scope.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("executeActivity");
        scope.Set(new
        {
            ActivityType = TypeCache<TActivity>.ShortName,
            ArgumentType = TypeCache<TArguments>.ShortName
        });

        if (_compensateAddress != null)
            scope.Add("compensateAddress", _compensateAddress);

        _executePipe.Probe(scope);
    }

    static bool IsCancellation(Exception exception)
    {
        return ConsumerIngressFailure.IsCancellation(exception);
    }
}
