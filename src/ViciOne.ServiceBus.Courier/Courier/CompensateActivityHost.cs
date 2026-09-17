using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Compensates the current routing-slip activity and advances or faults compensation.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityHost<TActivity, TLog> :
    IFilter<ConsumeContext<IRoutingSlip>>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly IPipe<CompensateContext<TLog>> _compensatePipe;

    /// <summary>Creates a host for the activity compensation pipeline.</summary>
    /// <param name="compensatePipe">The pipeline that deserializes the log, resolves the activity, and records its result.</param>
    public CompensateActivityHost(IPipe<CompensateContext<TLog>> compensatePipe)
    {
        ArgumentNullException.ThrowIfNull(compensatePipe);

        _compensatePipe = compensatePipe;
    }

    /// <summary>Compensates the newest compensation entry, evaluates its result, and advances the receive pipeline.</summary>
    /// <param name="context">The received routing slip whose newest compensation entry is processed.</param>
    /// <param name="next">The receive pipeline invoked after the compensation outcome is dispatched.</param>
    /// <returns>A task that completes after result dispatch and receive-pipeline continuation.</returns>
    public async Task SendAsync(ConsumeContext<IRoutingSlip> context, IPipe<ConsumeContext<IRoutingSlip>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        TimeProvider timeProvider = context.GetTimeProvider();
        long startedAt = timeProvider.GetTimestamp();

        StartedActivity? activity = CourierActivity.TryStartCompensation<TActivity, TLog>(context);
        var instrument = LogContext.Current?.TryStartCompensationMetrics<TActivity, TLog>(context);

        try
        {
            await CompensateCurrentActivityAsync(context, activity, instrument).ConfigureAwait(false);

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

    async Task CompensateCurrentActivityAsync(
        ConsumeContext<IRoutingSlip> context,
        StartedActivity? activity,
        MetricOperation? instrument)
    {
        CompensateContext<TLog> compensateContext = new HostCompensateContext<TLog>(context);
        CompensationResult result;

        LogContext.Debug?.Log("Compensate Activity: {TrackingNumber} ({Activity}, {Host})", compensateContext.TrackingNumber,
            TypeCache<TActivity>.ShortName, context.Advanced().ReceiveContext.InputAddress);

        try
        {
            await _compensatePipe.SendAsync(compensateContext).ConfigureAwait(false);

            result = compensateContext.Result
                ?? compensateContext.Failed(new ActivityCompensationException("The activity compensation did not return a result"));
        }
        catch (Exception exception) when (!IsCancellation(exception))
        {
            CompensationResult? recordedResult = compensateContext.Result;
            if (recordedResult == null || !recordedResult.IsFailed(out var failure) || failure != exception)
                result = compensateContext.Result = compensateContext.Failed(exception);
            else
                result = recordedResult;

            activity?.AddExceptionEvent(exception);

            instrument?.RecordException(exception);
        }

        await result.EvaluateAsync(context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds the hosted compensation pipeline and activity contract to the probe graph.</summary>
    /// <param name="context">The probe context that receives the compensation filter scope.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("compensateActivity");
        scope.Set(new
        {
            ActivityType = TypeCache<TActivity>.ShortName,
            LogType = TypeCache<TLog>.ShortName
        });

        _compensatePipe.Probe(scope);
    }

    static bool IsCancellation(Exception exception)
    {
        return exception is OperationCanceledException || exception.GetBaseException() is OperationCanceledException;
    }
}
