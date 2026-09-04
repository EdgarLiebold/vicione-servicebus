using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Sends the message through the repository using the specified saga policy.
/// </summary>
/// <typeparam name="TSaga">The saga type</typeparam>
/// <typeparam name="TMessage">The message type</typeparam>
public class CorrelatedSagaFilter<TSaga, TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _messagePipe;
    readonly ISagaPolicy<TSaga, TMessage> _policy;
    readonly ISagaRepository<TSaga> _sagaRepository;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sagaRepository">The saga repository value.</param>
    /// <param name="policy">The policy value.</param>
    /// <param name="messagePipe">The message pipe value.</param>
    public CorrelatedSagaFilter(ISagaRepository<TSaga> sagaRepository, ISagaPolicy<TSaga, TMessage> policy,
        IPipe<SagaConsumeContext<TSaga, TMessage>> messagePipe)
    {
        _sagaRepository = sagaRepository;
        _messagePipe = messagePipe;
        _policy = policy;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("saga");
        scope.Set(new { Correlation = "Id" });

        _sagaRepository.Probe(scope);

        _messagePipe.Probe(scope);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        TimeProvider timeProvider = context.GetTimeProvider();
        long startedAt = timeProvider.GetTimestamp();
        try
        {
            await _sagaRepository.SendAsync(context, _policy, _messagePipe).ConfigureAwait(false);

            await next.SendAsync(context).ConfigureAwait(false);

            await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TSaga>.ShortName).ConfigureAwait(false);
        }
        catch (Exception exception) when ((exception is OperationCanceledException || exception.GetBaseException() is OperationCanceledException)
                                          && !context.CancellationToken.IsCancellationRequested)
        {
            await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TSaga>.ShortName, exception).ConfigureAwait(false);

            throw new ConsumerCanceledException($"The operation was canceled by the saga: {TypeCache<TSaga>.ShortName}");
        }
        catch (Exception exception)
        {
            await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TSaga>.ShortName, exception).ConfigureAwait(false);

            throw;
        }
    }
}
