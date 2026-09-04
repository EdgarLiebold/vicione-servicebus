using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Creates a filter to send a query to the saga repository using the query factory and saga policy provided.
/// </summary>
/// <typeparam name="TSaga">The saga type</typeparam>
/// <typeparam name="TMessage">The message type</typeparam>
public class QuerySagaFilter<TSaga, TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _messagePipe;
    readonly ISagaPolicy<TSaga, TMessage> _policy;
    readonly ISagaQueryFactory<TSaga, TMessage> _queryFactory;
    readonly ISagaRepository<TSaga> _sagaRepository;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sagaRepository">The saga repository value.</param>
    /// <param name="policy">The policy value.</param>
    /// <param name="queryFactory">The query factory value.</param>
    /// <param name="messagePipe">The message pipe value.</param>
    public QuerySagaFilter(ISagaRepository<TSaga> sagaRepository, ISagaPolicy<TSaga, TMessage> policy,
        ISagaQueryFactory<TSaga, TMessage> queryFactory, IPipe<SagaConsumeContext<TSaga, TMessage>> messagePipe)
    {
        _sagaRepository = sagaRepository;
        _messagePipe = messagePipe;
        _policy = policy;
        _queryFactory = queryFactory;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("saga");
        scope.Set(new { Correlation = "Query" });

        _queryFactory.Probe(scope);
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
            if (_queryFactory.TryCreateQuery(context, out ISagaQuery<TSaga>? query))
            {
                await _sagaRepository.SendQueryAsync(context, query, _policy, _messagePipe).ConfigureAwait(false);

                await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TSaga>.ShortName).ConfigureAwait(false);
            }

            await next.SendAsync(context).ConfigureAwait(false);
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
