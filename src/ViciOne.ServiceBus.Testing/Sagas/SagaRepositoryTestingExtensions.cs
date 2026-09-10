using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Waits for observable saga repository state during tests.</summary>
public static class SagaRepositoryTestingExtensions
{
    static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(10);

    /// <summary>Waits until a saga with the specified correlation identifier exists.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga is found; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.WaitForSagaAsync(correlationId, timeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>Waits until a saga with the specified correlation identifier exists.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga is found; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is ILoadSagaRepository<TSaga> loadRepository)
            return loadRepository.WaitForSagaAsync(correlationId, timeout, timeProvider, cancellationToken);

        if (repository is IQuerySagaRepository<TSaga> queryRepository)
            return queryRepository.WaitForSagaAsync(correlationId, timeout, timeProvider, cancellationToken);

        throw new ArgumentException("The repository must support loading or querying sagas.", nameof(repository));
    }

    /// <summary>Waits until a loadable saga with the specified correlation identifier exists.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The load-capable saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga is found; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.WaitForSagaAsync(correlationId, timeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>Waits until a loadable saga with the specified correlation identifier exists.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The load-capable saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga is found; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        return PollAsync(
            async () => (await repository.LoadAsync(correlationId, cancellationToken).ConfigureAwait(false))?.CorrelationId,
            sagaId => sagaId.HasValue,
            timeout,
            timeProvider,
            cancellationToken);
    }

    /// <summary>Waits until a queryable saga with the specified correlation identifier exists.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The query-capable saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga is found; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.WaitForSagaAsync(correlationId, timeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>Waits until a queryable saga with the specified correlation identifier exists.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The query-capable saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga is found; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        var query = new SagaQuery<TSaga>(saga => saga.CorrelationId == correlationId);
        return PollAsync(
            async () => (Guid?)(await repository.FindAsync(query, cancellationToken).ConfigureAwait(false)).FirstOrDefault(),
            sagaId => sagaId.HasValue && sagaId.Value != Guid.Empty,
            timeout,
            timeProvider,
            cancellationToken);
    }

    /// <summary>Waits until a saga with the specified correlation identifier satisfies a condition.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="condition">The condition applied to the loaded saga.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the condition is satisfied; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId,
        Func<TSaga, bool> condition, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.WaitForSagaAsync(correlationId, condition, timeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>Waits until a saga with the specified correlation identifier satisfies a condition.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="condition">The condition applied to the loaded saga.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the condition is satisfied; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId,
        Func<TSaga, bool> condition, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is ILoadSagaRepository<TSaga> loadRepository)
            return loadRepository.WaitForSagaAsync(correlationId, condition, timeout, timeProvider, cancellationToken);

        throw new ArgumentException("The repository must support loading sagas.", nameof(repository));
    }

    /// <summary>Waits until a loadable saga with the specified correlation identifier satisfies a condition.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The load-capable saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="condition">The condition applied to the loaded saga.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the condition is satisfied; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId,
        Func<TSaga, bool> condition, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.WaitForSagaAsync(correlationId, condition, timeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>Waits until a loadable saga with the specified correlation identifier satisfies a condition.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The load-capable saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="condition">The condition applied to the loaded saga.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the condition is satisfied; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId,
        Func<TSaga, bool> condition, TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(condition);
        return PollAsync(async () =>
        {
            TSaga? saga = await repository.LoadAsync(correlationId, cancellationToken).ConfigureAwait(false);
            return saga != null && condition(saga) ? saga.CorrelationId : default(Guid?);
        }, sagaId => sagaId.HasValue, timeout, timeProvider, cancellationToken);
    }

    /// <summary>Waits until the saga with the specified correlation identifier is absent.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier after absence is confirmed; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaRemovalAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.WaitForSagaRemovalAsync(correlationId, timeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>Waits until the saga with the specified correlation identifier is absent.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier after absence is confirmed; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaRemovalAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is ILoadSagaRepository<TSaga> loadRepository)
            return loadRepository.WaitForSagaRemovalAsync(correlationId, timeout, timeProvider, cancellationToken);

        if (repository is IQuerySagaRepository<TSaga> queryRepository)
            return queryRepository.WaitForSagaRemovalAsync(correlationId, timeout, timeProvider, cancellationToken);

        throw new ArgumentException("The repository must support loading or querying sagas.", nameof(repository));
    }

    /// <summary>Waits until the loadable saga with the specified correlation identifier is absent.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The load-capable saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier after absence is confirmed; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaRemovalAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.WaitForSagaRemovalAsync(correlationId, timeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>Waits until the loadable saga with the specified correlation identifier is absent.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The load-capable saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier after absence is confirmed; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaRemovalAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        return PollAsync(
            async () => await repository.LoadAsync(correlationId, cancellationToken).ConfigureAwait(false) == null
                ? correlationId
                : default(Guid?),
            sagaId => sagaId.HasValue,
            timeout,
            timeProvider,
            cancellationToken);
    }

    /// <summary>Waits until the queryable saga with the specified correlation identifier is absent.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The query-capable saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier after absence is confirmed; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaRemovalAsync<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.WaitForSagaRemovalAsync(correlationId, timeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>Waits until the queryable saga with the specified correlation identifier is absent.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The query-capable saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier after absence is confirmed; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaRemovalAsync<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId,
        TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        var query = new SagaQuery<TSaga>(saga => saga.CorrelationId == correlationId);
        return PollAsync(
            async () => (await repository.FindAsync(query, cancellationToken).ConfigureAwait(false)).Any()
                ? default(Guid?)
                : correlationId,
            sagaId => sagaId.HasValue,
            timeout,
            timeProvider,
            cancellationToken);
    }

    /// <summary>Waits until at least one queryable saga satisfies a predicate.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>A matching correlation identifier, or <see langword="null"/> when the timeout expires.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this ISagaRepository<TSaga> repository,
        Expression<Func<TSaga, bool>> filter, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.WaitForSagaAsync(filter, timeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>Waits until at least one queryable saga satisfies a predicate.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>A matching correlation identifier, or <see langword="null"/> when the timeout expires.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this ISagaRepository<TSaga> repository,
        Expression<Func<TSaga, bool>> filter, TimeSpan timeout, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is IQuerySagaRepository<TSaga> queryRepository)
            return queryRepository.WaitForSagaAsync(filter, timeout, timeProvider, cancellationToken);

        throw new ArgumentException("The repository must support querying sagas.", nameof(repository));
    }

    /// <summary>Waits until at least one queryable saga satisfies a predicate.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The query-capable saga repository.</param>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>A matching correlation identifier, or <see langword="null"/> when the timeout expires.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this IQuerySagaRepository<TSaga> repository,
        Expression<Func<TSaga, bool>> filter, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.WaitForSagaAsync(filter, timeout, TimeProvider.System, cancellationToken);
    }

    /// <summary>Waits until at least one queryable saga satisfies a predicate.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="repository">The query-capable saga repository.</param>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>A matching correlation identifier, or <see langword="null"/> when the timeout expires.</returns>
    public static Task<Guid?> WaitForSagaAsync<TSaga>(this IQuerySagaRepository<TSaga> repository,
        Expression<Func<TSaga, bool>> filter, TimeSpan timeout, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(filter);
        var query = new SagaQuery<TSaga>(filter);
        return PollAsync(
            async () => (Guid?)(await repository.FindAsync(query, cancellationToken).ConfigureAwait(false)).FirstOrDefault(),
            sagaId => sagaId.HasValue && sagaId.Value != Guid.Empty,
            timeout,
            timeProvider,
            cancellationToken);
    }

    static async Task<Guid?> PollAsync(Func<Task<Guid?>> probe, Func<Guid?, bool> completed, TimeSpan timeout,
        TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (timeout < TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be non-negative.");

        cancellationToken.ThrowIfCancellationRequested();
        var startedAt = timeProvider.GetTimestamp();
        while (true)
        {
            Guid? result = await probe().ConfigureAwait(false);
            if (completed(result))
                return result;

            TimeSpan remaining = timeout - timeProvider.GetElapsedTime(startedAt);
            if (remaining <= TimeSpan.Zero)
                return null;

            await Task.Delay(remaining < _pollInterval ? remaining : _pollInterval, timeProvider, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
