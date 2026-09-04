using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

public static class ExtensionMethodsForSagas
{
    static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(10);

    public static Task<Guid?> ShouldContainSaga<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSaga(correlationId, timeout, TimeProvider.System);
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is ILoadSagaRepository<TSaga> loadSagaRepository)
            return loadSagaRepository.ShouldContainSaga(correlationId, timeout, timeProvider);

        if (repository is IQuerySagaRepository<TSaga> querySagaRepository)
            return querySagaRepository.ShouldContainSaga(correlationId, timeout, timeProvider);

        return TaskResults.Faulted<Guid?>(new ArgumentException("The repository must support loading or querying sagas", nameof(repository)));
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSaga(correlationId, timeout, TimeProvider.System);
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        return PollAsync(async () => (await repository.Load(correlationId).ConfigureAwait(false))?.CorrelationId,
            sagaId => sagaId.HasValue, timeout, timeProvider);
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSaga(correlationId, timeout, TimeProvider.System);
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        var query = new SagaQuery<TSaga>(x => x.CorrelationId == correlationId);
        return PollAsync(async () => (Guid?)(await repository.Find(query).ConfigureAwait(false)).SingleOrDefault(),
            sagaId => sagaId.HasValue && sagaId.Value != Guid.Empty, timeout, timeProvider);
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, Func<TSaga, bool> condition,
        TimeSpan timeout)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSaga(correlationId, condition, timeout, TimeProvider.System);
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, Func<TSaga, bool> condition,
        TimeSpan timeout, TimeProvider timeProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is ILoadSagaRepository<TSaga> loadSagaRepository)
            return loadSagaRepository.ShouldContainSaga(correlationId, condition, timeout, timeProvider);

        return TaskResults.Faulted<Guid?>(new ArgumentException("The repository must support loading sagas", nameof(repository)));
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, Func<TSaga, bool> condition,
        TimeSpan timeout)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSaga(correlationId, condition, timeout, TimeProvider.System);
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, Func<TSaga, bool> condition,
        TimeSpan timeout, TimeProvider timeProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(condition);
        return PollAsync(async () =>
        {
            TSaga saga = await repository.Load(correlationId).ConfigureAwait(false);
            return saga != null && condition(saga) ? saga.CorrelationId : default(Guid?);
        }, sagaId => sagaId.HasValue, timeout, timeProvider);
    }

    public static Task<Guid?> ShouldNotContainSaga<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout)
        where TSaga : class, ISaga
    {
        return repository.ShouldNotContainSaga(correlationId, timeout, TimeProvider.System);
    }

    public static Task<Guid?> ShouldNotContainSaga<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is ILoadSagaRepository<TSaga> loadSagaRepository)
            return loadSagaRepository.ShouldNotContainSaga(correlationId, timeout, timeProvider);

        if (repository is IQuerySagaRepository<TSaga> querySagaRepository)
            return querySagaRepository.ShouldNotContainSaga(correlationId, timeout, timeProvider);

        return TaskResults.Faulted<Guid?>(new ArgumentException("The repository must support loading or querying sagas", nameof(repository)));
    }

    public static Task<Guid?> ShouldNotContainSaga<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout)
        where TSaga : class, ISaga
    {
        return repository.ShouldNotContainSaga(correlationId, timeout, TimeProvider.System);
    }

    public static Task<Guid?> ShouldNotContainSaga<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        return PollAsync(async () => (await repository.Load(correlationId).ConfigureAwait(false))?.CorrelationId,
            sagaId => !sagaId.HasValue, timeout, timeProvider);
    }

    public static Task<Guid?> ShouldNotContainSaga<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout)
        where TSaga : class, ISaga
    {
        return repository.ShouldNotContainSaga(correlationId, timeout, TimeProvider.System);
    }

    public static Task<Guid?> ShouldNotContainSaga<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        var query = new SagaQuery<TSaga>(x => x.CorrelationId == correlationId);
        return PollAsync(async () => (Guid?)(await repository.Find(query).ConfigureAwait(false)).FirstOrDefault(),
            sagaId => !sagaId.HasValue || sagaId.Value == Guid.Empty, timeout, timeProvider);
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this ISagaRepository<TSaga> repository, Expression<Func<TSaga, bool>> filter,
        TimeSpan timeout)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSaga(filter, timeout, TimeProvider.System);
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this ISagaRepository<TSaga> repository, Expression<Func<TSaga, bool>> filter,
        TimeSpan timeout, TimeProvider timeProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is IQuerySagaRepository<TSaga> querySagaRepository)
            return querySagaRepository.ShouldContainSaga(filter, timeout, timeProvider);

        return TaskResults.Faulted<Guid?>(new ArgumentException("The repository must support querying sagas", nameof(repository)));
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this IQuerySagaRepository<TSaga> repository, Expression<Func<TSaga, bool>> filter,
        TimeSpan timeout)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSaga(filter, timeout, TimeProvider.System);
    }

    public static Task<Guid?> ShouldContainSaga<TSaga>(this IQuerySagaRepository<TSaga> repository, Expression<Func<TSaga, bool>> filter,
        TimeSpan timeout, TimeProvider timeProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(filter);
        var query = new SagaQuery<TSaga>(filter);
        return PollAsync(async () => (Guid?)(await repository.Find(query).ConfigureAwait(false)).SingleOrDefault(),
            sagaId => sagaId.HasValue && sagaId.Value != Guid.Empty, timeout, timeProvider);
    }

    static async Task<TResult> PollAsync<TResult>(Func<Task<TResult>> probe, Func<TResult, bool> completed, TimeSpan timeout,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (timeout <= TimeSpan.Zero)
            return default;

        var startedAt = timeProvider.GetTimestamp();
        TResult last = default;
        while (timeProvider.GetElapsedTime(startedAt) < timeout)
        {
            last = await probe().ConfigureAwait(false);
            if (completed(last))
                return last;

            await Task.Delay(_pollInterval, timeProvider, CancellationToken.None).ConfigureAwait(false);
        }

        return last;
    }
}
