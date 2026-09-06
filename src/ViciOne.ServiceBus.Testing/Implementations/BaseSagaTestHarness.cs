using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Provides a test harness for base saga test.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public abstract class BaseSagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="querySagaRepository">The query saga repository.</param>
    /// <param name="loadSagaRepository">The load saga repository.</param>
    /// <param name="testTimeout">The test timeout.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    protected BaseSagaTestHarness(IQuerySagaRepository<TSaga>? querySagaRepository, ILoadSagaRepository<TSaga>? loadSagaRepository, TimeSpan testTimeout,
        TimeProvider timeProvider)
    {
        QuerySagaRepository = querySagaRepository;
        LoadSagaRepository = loadSagaRepository;

        TestTimeout = testTimeout;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets the test timeout.</summary>
    protected TimeSpan TestTimeout { get; }
    /// <summary>Gets the time provider.</summary>
    protected TimeProvider TimeProvider { get; }

    /// <summary>Gets the query saga repository.</summary>
    protected IQuerySagaRepository<TSaga>? QuerySagaRepository { get; }
    /// <summary>Gets the load saga repository.</summary>
    protected ILoadSagaRepository<TSaga>? LoadSagaRepository { get; }

    /// <summary>Waits until a saga exists with the specified correlationId.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the exists outcome.</returns>
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

    /// <summary>Waits until at least one saga exists matching the specified filter.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the match outcome.</returns>
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

    /// <summary>Waits until the saga matching the specified correlationId does NOT exist.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the not exists outcome.</returns>
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

    /// <summary>Polls the configured source for available work.</summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="probe">The probe.</param>
    /// <param name="completed">The completed.</param>
    /// <param name="timeoutResult">The timeout result.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <returns>A task that produces the poll outcome.</returns>
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
