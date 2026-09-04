using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Provides an execute activity host implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class ExecuteActivityHost<TActivity, TArguments> :
    IFilter<ConsumeContext<RoutingSlip>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly Uri _compensateAddress;
    readonly IPipe<ExecuteContext<TArguments>> _executePipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="executePipe">The execute pipe value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public ExecuteActivityHost(IPipe<ExecuteContext<TArguments>> executePipe, Uri compensateAddress)
    {
        _executePipe = executePipe;
        _compensateAddress = compensateAddress;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ConsumeContext<RoutingSlip> context, IPipe<ConsumeContext<RoutingSlip>> next)
    {
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

                await result.EvaluateAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (executeContext.Result == null || !executeContext.Result.IsFaulted(out var faultException) || faultException != exception)
                    executeContext.Result = executeContext.Faulted(exception);

                await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TActivity>.ShortName, exception).ConfigureAwait(false);

                activity?.AddExceptionEvent(exception);
                instrument?.RecordException(exception);

                await executeContext.Result.EvaluateAsync().ConfigureAwait(false);
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

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
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
}
