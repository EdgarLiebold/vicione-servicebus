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

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSagaAsync(correlationId, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is ILoadSagaRepository<TSaga> loadSagaRepository)
            return loadSagaRepository.ShouldContainSagaAsync(correlationId, timeout, timeProvider, cancellationToken: cancellationToken);

        if (repository is IQuerySagaRepository<TSaga> querySagaRepository)
            return querySagaRepository.ShouldContainSagaAsync(correlationId, timeout, timeProvider, cancellationToken: cancellationToken);

        return TaskResults.FaultedAsync<Guid?>(new ArgumentException("The repository must support loading or querying sagas", nameof(repository)), cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSagaAsync(correlationId, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        return PollAsync(async () => (await repository.LoadAsync(correlationId, cancellationToken: cancellationToken).ConfigureAwait(false))?.CorrelationId,
            sagaId => sagaId.HasValue, timeout, timeProvider);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSagaAsync(correlationId, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        var query = new SagaQuery<TSaga>(x => x.CorrelationId == correlationId);
        return PollAsync(async () => (Guid?)(await repository.FindAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false)).SingleOrDefault(),
            sagaId => sagaId.HasValue && sagaId.Value != Guid.Empty, timeout, timeProvider);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, Func<TSaga, bool> condition,
        TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSagaAsync(correlationId, condition, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, Func<TSaga, bool> condition,
        TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is ILoadSagaRepository<TSaga> loadSagaRepository)
            return loadSagaRepository.ShouldContainSagaAsync(correlationId, condition, timeout, timeProvider, cancellationToken: cancellationToken);

        return TaskResults.FaultedAsync<Guid?>(new ArgumentException("The repository must support loading sagas", nameof(repository)), cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, Func<TSaga, bool> condition,
        TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSagaAsync(correlationId, condition, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, Func<TSaga, bool> condition,
        TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(condition);
        return PollAsync(async () =>
        {
            TSaga? saga = await repository.LoadAsync(correlationId, cancellationToken: cancellationToken).ConfigureAwait(false);
            return saga != null && condition(saga) ? saga.CorrelationId : default(Guid?);
        }, sagaId => sagaId.HasValue, timeout, timeProvider);
    }

    public static Task<Guid?> ShouldNotContainSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.ShouldNotContainSagaAsync(correlationId, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldNotContainSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is ILoadSagaRepository<TSaga> loadSagaRepository)
            return loadSagaRepository.ShouldNotContainSagaAsync(correlationId, timeout, timeProvider, cancellationToken: cancellationToken);

        if (repository is IQuerySagaRepository<TSaga> querySagaRepository)
            return querySagaRepository.ShouldNotContainSagaAsync(correlationId, timeout, timeProvider, cancellationToken: cancellationToken);

        return TaskResults.FaultedAsync<Guid?>(new ArgumentException("The repository must support loading or querying sagas", nameof(repository)), cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldNotContainSagaAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.ShouldNotContainSagaAsync(correlationId, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldNotContainSagaAsync<TSaga>(this ILoadSagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        return PollAsync(async () => (await repository.LoadAsync(correlationId, cancellationToken: cancellationToken).ConfigureAwait(false))?.CorrelationId,
            sagaId => !sagaId.HasValue, timeout, timeProvider);
    }

    public static Task<Guid?> ShouldNotContainSagaAsync<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.ShouldNotContainSagaAsync(correlationId, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldNotContainSagaAsync<TSaga>(this IQuerySagaRepository<TSaga> repository, Guid correlationId, TimeSpan timeout,
        TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        var query = new SagaQuery<TSaga>(x => x.CorrelationId == correlationId);
        return PollAsync(async () => (Guid?)(await repository.FindAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false)).FirstOrDefault(),
            sagaId => !sagaId.HasValue || sagaId.Value == Guid.Empty, timeout, timeProvider);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Expression<Func<TSaga, bool>> filter,
        TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSagaAsync(filter, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this ISagaRepository<TSaga> repository, Expression<Func<TSaga, bool>> filter,
        TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (repository is IQuerySagaRepository<TSaga> querySagaRepository)
            return querySagaRepository.ShouldContainSagaAsync(filter, timeout, timeProvider, cancellationToken: cancellationToken);

        return TaskResults.FaultedAsync<Guid?>(new ArgumentException("The repository must support querying sagas", nameof(repository)), cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this IQuerySagaRepository<TSaga> repository, Expression<Func<TSaga, bool>> filter,
        TimeSpan timeout, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        return repository.ShouldContainSagaAsync(filter, timeout, TimeProvider.System, cancellationToken: cancellationToken);
    }

    public static Task<Guid?> ShouldContainSagaAsync<TSaga>(this IQuerySagaRepository<TSaga> repository, Expression<Func<TSaga, bool>> filter,
        TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(filter);
        var query = new SagaQuery<TSaga>(filter);
        return PollAsync(async () => (Guid?)(await repository.FindAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false)).SingleOrDefault(),
            sagaId => sagaId.HasValue && sagaId.Value != Guid.Empty, timeout, timeProvider);
    }

    static async Task<Guid?> PollAsync(Func<Task<Guid?>> probe, Func<Guid?, bool> completed, TimeSpan timeout,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (timeout <= TimeSpan.Zero)
            return default;

        var startedAt = timeProvider.GetTimestamp();
        Guid? last = default;
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
