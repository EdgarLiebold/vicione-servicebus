using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a base saga test harness implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public abstract class BaseSagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="querySagaRepository">The query saga repository value.</param>
    /// <param name="loadSagaRepository">The load saga repository value.</param>
    /// <param name="testTimeout">The test timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    protected BaseSagaTestHarness(IQuerySagaRepository<TSaga>? querySagaRepository, ILoadSagaRepository<TSaga>? loadSagaRepository, TimeSpan testTimeout,
        TimeProvider timeProvider)
    {
        QuerySagaRepository = querySagaRepository;
        LoadSagaRepository = loadSagaRepository;

        TestTimeout = testTimeout;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Gets the test timeout value.
    /// </summary>
    protected TimeSpan TestTimeout { get; }
    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    protected TimeProvider TimeProvider { get; }

    /// <summary>
    /// Gets the query saga repository value.
    /// </summary>
    protected IQuerySagaRepository<TSaga>? QuerySagaRepository { get; }
    /// <summary>
    /// Gets the load saga repository value.
    /// </summary>
    protected ILoadSagaRepository<TSaga>? LoadSagaRepository { get; }

    /// <summary>
    /// Waits until a saga exists with the specified correlationId
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task<Guid?> ExistsAsync(Guid correlationId, TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        if (LoadSagaRepository == null)
            throw new InvalidOperationException("The repository does not support Load operations");

        return await PollAsync(
            async () => (await LoadSagaRepository.LoadAsync(correlationId, cancellationToken: cancellationToken).ConfigureAwait(false))?.CorrelationId,
            sagaId => sagaId.HasValue,
            default(Guid?),
            timeout).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits until at least one saga exists matching the specified filter
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task<IList<Guid>> MatchAsync(Expression<Func<TSaga, bool>> filter, TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        if (QuerySagaRepository == null)
            throw new InvalidOperationException("The repository does not support Query operations");

        var query = new SagaQuery<TSaga>(filter);

        return await PollAsync(
            async () => (IList<Guid>)(await QuerySagaRepository.FindAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false)).ToList(),
            sagas => sagas.Count > 0,
            new List<Guid>(),
            timeout).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits until the saga matching the specified correlationId does NOT exist
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public async Task<Guid?> NotExistsAsync(Guid correlationId, TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        if (LoadSagaRepository == null)
            throw new InvalidOperationException("The repository does not support Load operations");

        TSaga? saga = await PollAsync<TSaga?>(
            () => LoadSagaRepository.LoadAsync(correlationId, cancellationToken: cancellationToken),
            instance => instance == null,
            default,
            timeout).ConfigureAwait(false);

        return saga?.CorrelationId;
    }

    /// <summary>
    /// Performs the poll operation.
    /// </summary>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="probe">The probe value.</param>
    /// <param name="completed">The completed value.</param>
    /// <param name="timeoutResult">The timeout result value.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    protected async Task<TResult> PollAsync<TResult>(Func<Task<TResult>> probe, Func<TResult, bool> completed, TResult timeoutResult,
        TimeSpan? timeout = default)
    {
        var effectiveTimeout = timeout ?? TestTimeout;
        if (effectiveTimeout <= TimeSpan.Zero)
            return timeoutResult;

        var startedAt = TimeProvider.GetTimestamp();
        while (TimeProvider.GetElapsedTime(startedAt) < effectiveTimeout)
        {
            TResult result = await probe().ConfigureAwait(false);
            if (completed(result))
                return result;

            await Task.Delay(TimeSpan.FromMilliseconds(10), TimeProvider).ConfigureAwait(false);
        }

        return timeoutResult;
    }
}
