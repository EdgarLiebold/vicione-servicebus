using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Creates a filter to send a query to the saga repository using the query factory and saga policy provided.</summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public class QuerySagaFilter<TSaga, TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _messagePipe;
    readonly ISagaPolicy<TSaga, TMessage> _policy;
    readonly ISagaQueryFactory<TSaga, TMessage> _queryFactory;
    readonly ISagaRepository<TSaga> _sagaRepository;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sagaRepository">The saga repository.</param>
    /// <param name="policy">The policy.</param>
    /// <param name="queryFactory">The query factory.</param>
    /// <param name="messagePipe">The message pipe.</param>
    public QuerySagaFilter(ISagaRepository<TSaga> sagaRepository, ISagaPolicy<TSaga, TMessage> policy,
        ISagaQueryFactory<TSaga, TMessage> queryFactory, IPipe<SagaConsumeContext<TSaga, TMessage>> messagePipe)
    {
        _sagaRepository = sagaRepository ?? throw new ArgumentNullException(nameof(sagaRepository));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _queryFactory = queryFactory ?? throw new ArgumentNullException(nameof(queryFactory));
        _messagePipe = messagePipe ?? throw new ArgumentNullException(nameof(messagePipe));
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("saga");
        scope.Set(new { Correlation = "Query" });

        _queryFactory.Probe(scope);
        _sagaRepository.Probe(scope);

        _messagePipe.Probe(scope);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        TimeProvider timeProvider = context.GetTimeProvider();
        long startedAt = timeProvider.GetTimestamp();
        ISagaQuery<TSaga>? query;
        bool hasQuery;
        try
        {
            hasQuery = _queryFactory.TryCreateQuery(context, out query);
        }
        catch (Exception exception)
        {
            await NotifyFaultedAndRethrowAsync(context, timeProvider, startedAt, exception).ConfigureAwait(false);
            return;
        }

        if (!hasQuery)
        {
            await next.SendAsync(context).ConfigureAwait(false);
            return;
        }

        try
        {
            if (query is null)
                throw new InvalidOperationException("The saga query factory reported success without returning a query.");

            await _sagaRepository.SendQueryAsync(context, query, _policy, _messagePipe).ConfigureAwait(false);

            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await NotifyFaultedAndRethrowAsync(context, timeProvider, startedAt, exception).ConfigureAwait(false);
            return;
        }

        await context.NotifyConsumedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TSaga>.ShortName).ConfigureAwait(false);
    }

    static async Task NotifyFaultedAndRethrowAsync(ConsumeContext<TMessage> context, TimeProvider timeProvider, long startedAt,
        Exception exception)
    {
        Exception operationException = exception is ConsumerCanceledException
            ? exception
            : ConsumerIngressFailure.IsUnexpectedCancellation(exception, context.CancellationToken)
            ? new ConsumerCanceledException($"The operation was canceled by the saga: {TypeCache<TSaga>.ShortName}", exception)
            : exception;

        try
        {
            await context.NotifyFaultedAsync(timeProvider.GetElapsedTime(startedAt), TypeCache<TSaga>.ShortName, exception)
                .ConfigureAwait(false);
        }
        catch (Exception observerException)
        {
            throw new AggregateException(operationException, observerException);
        }

        ExceptionDispatchInfo.Throw(operationException);
    }

}
