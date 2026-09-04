using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// For queries that load the actual saga instances
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class LoadedSagaRepositoryQueryContext<TSaga, TMessage> :
    ConsumeContextProxy<TMessage>,
    SagaRepositoryQueryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IDictionary<Guid, TSaga> _index;
    readonly SagaRepositoryContext<TSaga, TMessage> _repositoryContext;

    public LoadedSagaRepositoryQueryContext(SagaRepositoryContext<TSaga, TMessage> repositoryContext, IEnumerable<TSaga> instances)
        : base(repositoryContext)
    {
        _repositoryContext = repositoryContext;

        _index = instances.ToDictionary(x => x.CorrelationId);
    }

    public int Count => _index.Count;

    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.AddAsync(instance, cancellationToken: cancellationToken);
    }

    public Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.InsertAsync(instance, cancellationToken: cancellationToken);
    }

    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (_index.TryGetValue(correlationId, out var instance))
            return await _repositoryContext.CreateSagaConsumeContextAsync(_repositoryContext, instance, SagaConsumeContextMode.Load)
                .ConfigureAwait(false);

        return await _repositoryContext.LoadAsync(correlationId, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.SaveAsync(context, cancellationToken: cancellationToken);
    }

    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.DiscardAsync(context, cancellationToken: cancellationToken);
    }

    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.UndoAsync(context, cancellationToken: cancellationToken);
    }

    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.UpdateAsync(context, cancellationToken: cancellationToken);
    }

    public Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.DeleteAsync(context, cancellationToken: cancellationToken);
    }

    public IEnumerator<Guid> GetEnumerator()
    {
        return _index.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        return _repositoryContext.CreateSagaConsumeContextAsync(consumeContext, instance, mode);
    }
}


/// <summary>
/// For queries that load the actual saga instances
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public class LoadedSagaRepositoryQueryContext<TSaga> :
    BasePipeContext,
    SagaRepositoryQueryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly IDictionary<Guid, TSaga> _index;
    readonly QuerySagaRepositoryContext<TSaga> _querySagaRepositoryContext;

    public LoadedSagaRepositoryQueryContext(QuerySagaRepositoryContext<TSaga> querySagaRepositoryContext, IEnumerable<TSaga> instances)
        : base(querySagaRepositoryContext)
    {
        _querySagaRepositoryContext = querySagaRepositoryContext;

        _index = instances.ToDictionary(x => x.CorrelationId);
    }

    public int Count => _index.Count;

    public Task<SagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
    {
        return _querySagaRepositoryContext.QueryAsync(query, cancellationToken);
    }

    public IEnumerator<Guid> GetEnumerator()
    {
        return _index.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
