using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Dispatches the ConsumeContext to the consumer method for the specified message type.</summary>
/// <typeparam name="TSaga">The consumer type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public class ObservesSagaMessageFilter<TSaga, TMessage> :
    ISagaMessageFilter<TSaga, TMessage>
    where TSaga : class, ISaga, IObserves<TMessage, TSaga>
    where TMessage : class
{
    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("observes");
        scope.Add("method", $"Consume({TypeCache<TMessage>.ShortName} message)");
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        StartedActivity? activity = LogContext.Current?.StartSagaActivity(context);
        var instrument = LogContext.Current?.StartSagaInstrument(context);
        try
        {
            await context.Saga.ConsumeAsync(context).ConfigureAwait(false);

            await next.SendAsync(context).ConfigureAwait(false);
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
