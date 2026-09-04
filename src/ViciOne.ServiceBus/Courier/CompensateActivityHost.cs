using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Courier;

public class CompensateActivityHost<TActivity, TLog> :
    IFilter<ConsumeContext<RoutingSlip>>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly IPipe<CompensateContext<TLog>> _compensatePipe;

    public CompensateActivityHost(IPipe<CompensateContext<TLog>> compensatePipe)
    {
        _compensatePipe = compensatePipe;
    }

    public async Task SendAsync(ConsumeContext<RoutingSlip> context, IPipe<ConsumeContext<RoutingSlip>> next)
    {
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

                await result.EvaluateAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TActivity>.ShortName, exception).ConfigureAwait(false);

                activity?.AddExceptionEvent(exception);

                instrument?.RecordException(exception);

                await compensateContext.Failed(exception).EvaluateAsync().ConfigureAwait(false);
            }

            await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TActivity>.ShortName).ConfigureAwait(false);

            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception) when ((exception is OperationCanceledException || exception.GetBaseException() is OperationCanceledException)
                                          && !context.CancellationToken.IsCancellationRequested)
        {
            await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TActivity>.ShortName, exception).ConfigureAwait(false);

            activity?.AddExceptionEvent(exception);

            instrument?.RecordException(exception);

            throw new ConsumerCanceledException($"The operation was canceled by the activity: {TypeCache<TActivity>.ShortName}");
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

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("compensateActivity");
        scope.Set(new
        {
            ActivityType = TypeCache<TActivity>.ShortName,
            LogType = TypeCache<TLog>.ShortName
        });

        _compensatePipe.Probe(scope);
    }
}
