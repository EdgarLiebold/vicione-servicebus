using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Invokes an existing saga selected by its observation expression.</summary>
/// <typeparam name="TSaga">The observing saga state.</typeparam>
/// <typeparam name="TMessage">The observed message.</typeparam>
public class ObservesSagaMessageFilter<TSaga, TMessage> :
    ISagaMessageFilter<TSaga, TMessage>
    where TSaga : class, ISaga, IObserves<TMessage, TSaga>
    where TMessage : class
{
    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("observes");
        scope.Add("method", $"Consume({TypeCache<TMessage>.ShortName} message)");
    }

    /// <summary>Invokes the saga and then continues the saga-message pipeline.</summary>
    /// <param name="context">The selected saga instance and observed message.</param>
    /// <param name="next">The pipeline stage invoked after the saga consumes the message.</param>
    /// <returns>A task that completes after the saga and continuation finish.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is null.</exception>
    /// <exception cref="OperationCanceledException">
    /// The delivery is cancelled, or the saga or continuation reports cancellation.
    /// </exception>
    /// <exception cref="InvalidOperationException">The saga or continuation returns a null task.</exception>
    public async Task SendAsync(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        context.CancellationToken.ThrowIfCancellationRequested();

        StartedActivity? activity = SagaActivity.TryStart(context);
        var instrument = LogContext.Current?.TryStartSagaMetrics(context);
        try
        {
            Task consumeTask = context.Saga.ConsumeAsync(context)
                ?? throw new InvalidOperationException("The saga returned a null task from ConsumeAsync.");
            await consumeTask.ConfigureAwait(false);

            context.CancellationToken.ThrowIfCancellationRequested();

            Task nextTask = next.SendAsync(context)
                ?? throw new InvalidOperationException("The saga-message continuation returned a null task from SendAsync.");
            await nextTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            activity?.AddExceptionEvent(ex);
            instrument?.RecordException(ex);

            throw;
        }
        finally
        {
            activity?.Stop();
            instrument?.Complete();
        }
    }
}
