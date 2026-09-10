using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides repository polling shared by saga test harnesses.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public abstract class BaseSagaTestHarness<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates a harness over the repository capabilities supplied by the persistence provider.</summary>
    /// <param name="querySagaRepository">The optional repository query capability.</param>
    /// <param name="loadSagaRepository">The optional repository load capability.</param>
    /// <param name="testTimeout">The default repository polling timeout.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    protected BaseSagaTestHarness(IQuerySagaRepository<TSaga>? querySagaRepository,
        ILoadSagaRepository<TSaga>? loadSagaRepository, TimeSpan testTimeout, TimeProvider timeProvider)
    {
        if (testTimeout <= TimeSpan.Zero || testTimeout == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(testTimeout), testTimeout, "The timeout must be greater than zero.");

        QuerySagaRepository = querySagaRepository;
        LoadSagaRepository = loadSagaRepository;

        TestTimeout = testTimeout;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets the default repository polling timeout.</summary>
    protected TimeSpan TestTimeout { get; }
    /// <summary>Gets the time provider used by polling delays.</summary>
    protected TimeProvider TimeProvider { get; }

    /// <summary>Gets the optional repository query capability.</summary>
    protected IQuerySagaRepository<TSaga>? QuerySagaRepository { get; }
    /// <summary>Gets the optional repository load capability.</summary>
    protected ILoadSagaRepository<TSaga>? LoadSagaRepository { get; }

    /// <summary>Waits until a saga with the specified correlation identifier exists.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use <see cref="TestTimeout"/>.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga is found; otherwise, <see langword="null"/>.</returns>
    public async Task<Guid?> WaitForSagaAsync(Guid correlationId, TimeSpan? timeout = default,
        CancellationToken cancellationToken = default)
    {
        if (LoadSagaRepository == null)
            throw new InvalidOperationException($"The {typeof(TSaga).Name} repository does not support loading sagas by identifier.");

        return await PollAsync(
            async () => (await LoadSagaRepository.LoadAsync(correlationId, cancellationToken: cancellationToken).ConfigureAwait(false))?.CorrelationId,
            sagaId => sagaId.HasValue,
            default(Guid?),
            timeout,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Waits until at least one saga matches the specified predicate.</summary>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use <see cref="TestTimeout"/>.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The matching saga correlation identifiers, or an empty list when the timeout expires.</returns>
    public async Task<IReadOnlyList<Guid>> WaitForSagasAsync(Expression<Func<TSaga, bool>> filter, TimeSpan? timeout = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (QuerySagaRepository == null)
            throw new InvalidOperationException($"The {typeof(TSaga).Name} repository does not support querying sagas.");

        var query = new SagaQuery<TSaga>(filter);

        return await PollAsync(
            async () => (IReadOnlyList<Guid>)(await QuerySagaRepository.FindAsync(query, cancellationToken: cancellationToken)
                .ConfigureAwait(false)).ToList(),
            sagas => sagas.Count > 0,
            Array.Empty<Guid>(),
            timeout,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Waits until the saga with the specified correlation identifier does not exist.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use <see cref="TestTimeout"/>.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier after absence is confirmed; otherwise, <see langword="null"/>.</returns>
    public async Task<Guid?> WaitForSagaRemovalAsync(Guid correlationId, TimeSpan? timeout = default,
        CancellationToken cancellationToken = default)
    {
        if (LoadSagaRepository == null)
            throw new InvalidOperationException($"The {typeof(TSaga).Name} repository does not support loading sagas by identifier.");

        return await PollAsync(
            async () => await LoadSagaRepository.LoadAsync(correlationId, cancellationToken: cancellationToken).ConfigureAwait(false) == null
                ? correlationId
                : default(Guid?),
            sagaId => sagaId.HasValue,
            default(Guid?),
            timeout,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Polls a repository probe until its completion predicate succeeds or its deadline expires.</summary>
    /// <typeparam name="TResult">The probe result type.</typeparam>
    /// <param name="probe">The operation that reads the current repository state.</param>
    /// <param name="completed">The predicate that identifies the requested state.</param>
    /// <param name="timeoutResult">The result returned when the polling deadline expires.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use <see cref="TestTimeout"/>.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The successful probe result, or <paramref name="timeoutResult"/> when the deadline expires.</returns>
    protected async Task<TResult> PollAsync<TResult>(Func<Task<TResult>> probe, Func<TResult, bool> completed, TResult timeoutResult,
        TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(completed);
        cancellationToken.ThrowIfCancellationRequested();

        var effectiveTimeout = timeout ?? TestTimeout;
        if (effectiveTimeout < TimeSpan.Zero || effectiveTimeout == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be non-negative.");

        var startedAt = TimeProvider.GetTimestamp();
        while (true)
        {
            TResult result = await probe().ConfigureAwait(false);
            if (completed(result))
                return result;

            TimeSpan remaining = effectiveTimeout - TimeProvider.GetElapsedTime(startedAt);
            if (remaining <= TimeSpan.Zero)
                return timeoutResult;

            TimeSpan pollingDelay = TimeSpan.FromMilliseconds(10);
            await Task.Delay(remaining < pollingDelay ? remaining : pollingDelay, TimeProvider, cancellationToken).ConfigureAwait(false);
        }
    }
}
