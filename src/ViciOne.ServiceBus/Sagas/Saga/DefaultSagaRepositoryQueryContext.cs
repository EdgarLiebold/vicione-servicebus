using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

public class DefaultSagaRepositoryQueryContext<TSaga, TMessage> :
    ConsumeContextProxy<TMessage>,
    SagaRepositoryQueryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly SagaRepositoryContext<TSaga, TMessage> _context;
    readonly IList<Guid> _results;

    public DefaultSagaRepositoryQueryContext(SagaRepositoryContext<TSaga, TMessage> context, IList<Guid> results)
        : base(context)
    {
        _context = context;
        _results = results;
    }

    public int Count => _results.Count;

    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _context.AddAsync(instance, cancellationToken: cancellationToken);
    }

    public Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _context.InsertAsync(instance, cancellationToken: cancellationToken);
    }

    public Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _context.LoadAsync(correlationId, cancellationToken: cancellationToken);
    }

    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.SaveAsync(context, cancellationToken: cancellationToken);
    }

    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.DiscardAsync(context, cancellationToken: cancellationToken);
    }

    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.UndoAsync(context, cancellationToken: cancellationToken);
    }

    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.UpdateAsync(context, cancellationToken: cancellationToken);
    }

    public Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.DeleteAsync(context, cancellationToken: cancellationToken);
    }

    public IEnumerator<Guid> GetEnumerator()
    {
        return _results.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        return _context.CreateSagaConsumeContextAsync(consumeContext, instance, mode);
    }
}


public class DefaultSagaRepositoryQueryContext<TSaga> :
    ProxyPipeContext,
    SagaRepositoryQueryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly QuerySagaRepositoryContext<TSaga> _queryContext;
    readonly IList<Guid> _results;

    public DefaultSagaRepositoryQueryContext(QuerySagaRepositoryContext<TSaga> queryContext, IList<Guid> results)
        : base(queryContext)
    {
        _queryContext = queryContext;
        _results = results;
    }

    public int Count => _results.Count;

    public Task<SagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken)
    {
        return _queryContext.QueryAsync(query, cancellationToken);
    }

    public IEnumerator<Guid> GetEnumerator()
    {
        return _results.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
