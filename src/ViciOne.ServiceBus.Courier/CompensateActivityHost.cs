using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Compensates the current routing-slip activity and advances or faults compensation.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityHost<TActivity, TLog> :
    IFilter<ConsumeContext<RoutingSlip>>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly IPipe<CompensateContext<TLog>> _compensatePipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="compensatePipe">The compensate pipe.</param>
    public CompensateActivityHost(IPipe<CompensateContext<TLog>> compensatePipe)
    {
        ArgumentNullException.ThrowIfNull(compensatePipe);

        _compensatePipe = compensatePipe;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ConsumeContext<RoutingSlip> context, IPipe<ConsumeContext<RoutingSlip>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        TimeProvider timeProvider = context.GetTimeProvider();
        long startedAt = timeProvider.GetTimestamp();

        StartedActivity? activity = LogContext.Current?.StartCompensateActivity<TActivity, TLog>(context);
        var instrument = LogContext.Current?.StartActivityCompensateInstrument<TActivity, TLog>(context);

        try
        {
            CompensateContext<TLog> compensateContext = new HostCompensateContext<TLog>(context);

            LogContext.Debug?.Log("Compensate Activity: {TrackingNumber} ({Activity}, {Host})", compensateContext.TrackingNumber,
                TypeCache<TActivity>.ShortName, context.Advanced().ReceiveContext.InputAddress);

            try
            {
                await _compensatePipe.SendAsync(compensateContext).ConfigureAwait(false);

                var result = compensateContext.Result
                    ?? compensateContext.Failed(new ActivityCompensationException("The activity compensation did not return a result"));

                await result.EvaluateAsync(context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!IsCancellation(exception))
            {
                activity?.AddExceptionEvent(exception);

                instrument?.RecordException(exception);

                await compensateContext.Failed(exception).EvaluateAsync(context.CancellationToken).ConfigureAwait(false);
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

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
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
