using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Invokes an existing saga that continues with the correlated message.</summary>
/// <typeparam name="TSaga">The continuing saga state.</typeparam>
/// <typeparam name="TMessage">The correlated continuing message.</typeparam>
public class OrchestratesSagaMessageFilter<TSaga, TMessage> :
    ISagaMessageFilter<TSaga, TMessage>
    where TSaga : class, ISaga, IOrchestrates<TMessage>
    where TMessage : class, ICorrelatedBy<Guid>
{
    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("orchestrates");
        scope.Add("method", $"Consume({TypeCache<TMessage>.ShortName} message)");
    }

    /// <summary>Invokes the saga and then continues the saga-message pipeline.</summary>
    /// <param name="context">The saga instance and correlated message.</param>
    /// <param name="next">The pipeline stage invoked after the saga consumes the message.</param>
    /// <returns>A task that completes after the saga and continuation finish.</returns>
    public async Task SendAsync(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        StartedActivity? activity = SagaActivity.TryStart(context);
        var instrument = LogContext.Current?.TryStartSagaMetrics(context);
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
