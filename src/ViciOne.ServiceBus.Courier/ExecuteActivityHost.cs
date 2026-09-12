using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Executes the current routing-slip activity and advances or faults the routing slip.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityHost<TActivity, TArguments> :
    IFilter<ConsumeContext<RoutingSlip>>
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
    public async Task SendAsync(ConsumeContext<RoutingSlip> context, IPipe<ConsumeContext<RoutingSlip>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        TimeProvider timeProvider = context.GetTimeProvider();
        long startedAt = timeProvider.GetTimestamp();

        StartedActivity? activity = LogContext.Current?.StartExecuteActivity<TActivity, TArguments>(context);
        var instrument = LogContext.Current?.StartActivityExecuteInstrument<TActivity, TArguments>(context);

        try
        {
            ExecuteContext<TArguments> executeContext = new HostExecuteContext<TArguments>(_compensateAddress, context);

            LogContext.Debug?.Log("Execute Activity: {TrackingNumber} ({Activity}, {Host})", executeContext.TrackingNumber,
                TypeCache<TActivity>.ShortName, context.Advanced().ReceiveContext.InputAddress);

            try
            {
                await _executePipe.SendAsync(executeContext).ConfigureAwait(false);

                var result = executeContext.Result
                    ?? executeContext.Faulted(new ActivityExecutionException("The activity execute did not return a result"));

                await result.EvaluateAsync(context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!IsCancellation(exception))
            {
                if (executeContext.Result == null || !executeContext.Result.IsFaulted(out var faultException) || faultException != exception)
                    executeContext.Result = executeContext.Faulted(exception);

                activity?.AddExceptionEvent(exception);
                instrument?.RecordException(exception);

                await executeContext.Result.EvaluateAsync(context.CancellationToken).ConfigureAwait(false);
            }

            await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TActivity>.ShortName).ConfigureAwait(false);

            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception) when ((exception is OperationCanceledException || exception.GetBaseException() is OperationCanceledException)
                                          && !context.CancellationToken.IsCancellationRequested)
        {
            var cancellation = new ConsumerCanceledException(
                $"The operation was canceled by the activity: {TypeCache<TActivity>.ShortName}", exception);

            await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TActivity>.ShortName, cancellation).ConfigureAwait(false);

            activity?.AddExceptionEvent(exception);

            instrument?.RecordException(exception);

            throw cancellation;
        }
        catch (Exception exception)
        {
            await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TActivity>.ShortName, exception).ConfigureAwait(false);

            activity?.AddExceptionEvent(exception);

            instrument?.RecordException(exception);

            throw;
        }
        finally
        {
            activity?.Stop();
            instrument?.Complete();
        }
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
        return exception is OperationCanceledException || exception.GetBaseException() is OperationCanceledException;
    }
}
