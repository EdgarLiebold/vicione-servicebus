using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Dispatches correlated messages through a storage-specific saga repository context.</summary>
/// <typeparam name="TSaga">The saga state handled by the repository.</typeparam>
public sealed class SagaRepository<TSaga> :
    ISagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaRepositoryContextFactory<TSaga> _repositoryContextFactory;

    /// <summary>Creates a repository that supports message dispatch.</summary>
    /// <param name="repositoryContextFactory">The factory that opens storage-specific dispatch contexts.</param>
    public SagaRepository(ISagaRepositoryContextFactory<TSaga> repositoryContextFactory)
    {
        _repositoryContextFactory = repositoryContextFactory
            ?? throw new ArgumentNullException(nameof(repositoryContextFactory));
    }

    /// <summary>Creates a dispatch repository that also supports loading by correlation identifier.</summary>
    /// <param name="repositoryContextFactory">The factory that opens storage-specific dispatch contexts.</param>
    /// <param name="loadRepositoryContextFactory">The factory that opens storage-specific load contexts.</param>
    /// <returns>A repository exposing dispatch and load capabilities.</returns>
    public static ILoadableSagaRepository<TSaga> CreateLoadable(
        ISagaRepositoryContextFactory<TSaga> repositoryContextFactory,
        ILoadSagaRepositoryContextFactory<TSaga> loadRepositoryContextFactory)
    {
        ArgumentNullException.ThrowIfNull(repositoryContextFactory);
        ArgumentNullException.ThrowIfNull(loadRepositoryContextFactory);

        return new LoadableSagaRepository(repositoryContextFactory, loadRepositoryContextFactory);
    }

    /// <summary>Creates a dispatch repository that also supports querying and loading saga state.</summary>
    /// <param name="repositoryContextFactory">The factory that opens storage-specific dispatch contexts.</param>
    /// <param name="queryRepositoryContextFactory">The factory that opens storage-specific query contexts.</param>
    /// <param name="loadRepositoryContextFactory">The factory that opens storage-specific load contexts.</param>
    /// <returns>A repository exposing dispatch, query, and load capabilities.</returns>
    public static IQueryableSagaRepository<TSaga> CreateQueryable(
        ISagaRepositoryContextFactory<TSaga> repositoryContextFactory,
        IQuerySagaRepositoryContextFactory<TSaga> queryRepositoryContextFactory,
        ILoadSagaRepositoryContextFactory<TSaga> loadRepositoryContextFactory)
    {
        ArgumentNullException.ThrowIfNull(repositoryContextFactory);
        ArgumentNullException.ThrowIfNull(queryRepositoryContextFactory);
        ArgumentNullException.ThrowIfNull(loadRepositoryContextFactory);

        return new QueryableLoadableSagaRepository(
            repositoryContextFactory,
            queryRepositoryContextFactory,
            loadRepositoryContextFactory);
    }

    /// <summary>Writes the repository's storage-specific diagnostic structure.</summary>
    /// <param name="context">The diagnostic context to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("sagaRepository");
        _repositoryContextFactory.Probe(scope);
    }

    /// <summary>Dispatches a message to the saga identified by the consume context's correlation identifier.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consumed message and its correlation metadata.</param>
    /// <param name="policy">The policy that controls saga creation, use, and removal.</param>
    /// <param name="next">The saga pipeline invoked for the selected instance.</param>
    /// <returns>A task representing repository dispatch.</returns>
    public Task SendAsync<T>(
        ConsumeContext<T> context,
        ISagaPolicy<TSaga, T> policy,
        IPipe<SagaConsumeContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(next);

        var correlationId = context.CorrelationId
            ?? throw new SagaException("The CorrelationId was not specified", typeof(TSaga), typeof(T));

        return _repositoryContextFactory.SendAsync(context, new SendSagaPipe<TSaga, T>(policy, next, correlationId))
            ?? Task.FromException(new InvalidOperationException("The saga repository context factory returned a null task."));
    }

    /// <summary>Dispatches a message to every saga selected by a repository query.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consumed message.</param>
    /// <param name="query">The storage-independent saga predicate.</param>
    /// <param name="policy">The policy that controls saga use and removal.</param>
    /// <param name="next">The saga pipeline invoked for each selected instance.</param>
    /// <returns>A task representing query-based repository dispatch.</returns>
    public Task SendQueryAsync<T>(
        ConsumeContext<T> context,
        ISagaQuery<TSaga> query,
        ISagaPolicy<TSaga, T> policy,
        IPipe<SagaConsumeContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(next);

        return _repositoryContextFactory.SendQueryAsync(context, query, new SendQuerySagaPipe<TSaga, T>(policy, next))
            ?? Task.FromException(new InvalidOperationException("The saga repository context factory returned a null task."));
    }

    sealed class LoadableSagaRepository :
        ILoadableSagaRepository<TSaga>
    {
        readonly SagaRepository<TSaga> _dispatchRepository;
        readonly LoadSagaRepository<TSaga> _loadRepository;

        public LoadableSagaRepository(
            ISagaRepositoryContextFactory<TSaga> repositoryContextFactory,
            ILoadSagaRepositoryContextFactory<TSaga> loadRepositoryContextFactory)
        {
            _dispatchRepository = new SagaRepository<TSaga>(repositoryContextFactory);
            _loadRepository = new LoadSagaRepository<TSaga>(loadRepositoryContextFactory);
        }

        public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
            _loadRepository.LoadAsync(correlationId, cancellationToken);

        public Task SendAsync<T>(
            ConsumeContext<T> context,
            ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class =>
            _dispatchRepository.SendAsync(context, policy, next);

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<TSaga> query,
            ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class =>
            _dispatchRepository.SendQueryAsync(context, query, policy, next);

        public void Probe(ProbeContext context)
        {
            _dispatchRepository.Probe(context);
            _loadRepository.Probe(context);
        }
    }

    sealed class QueryableLoadableSagaRepository :
        IQueryableSagaRepository<TSaga>
    {
        readonly SagaRepository<TSaga> _dispatchRepository;
        readonly LoadSagaRepository<TSaga> _loadRepository;
        readonly QuerySagaRepository<TSaga> _queryRepository;

        public QueryableLoadableSagaRepository(
            ISagaRepositoryContextFactory<TSaga> repositoryContextFactory,
            IQuerySagaRepositoryContextFactory<TSaga> queryRepositoryContextFactory,
            ILoadSagaRepositoryContextFactory<TSaga> loadRepositoryContextFactory)
        {
            _dispatchRepository = new SagaRepository<TSaga>(repositoryContextFactory);
            _queryRepository = new QuerySagaRepository<TSaga>(queryRepositoryContextFactory);
            _loadRepository = new LoadSagaRepository<TSaga>(loadRepositoryContextFactory);
        }

        public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default) =>
            _loadRepository.LoadAsync(correlationId, cancellationToken);

        public Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default) =>
            _queryRepository.FindAsync(query, cancellationToken);

        public Task SendAsync<T>(
            ConsumeContext<T> context,
            ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class =>
            _dispatchRepository.SendAsync(context, policy, next);

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<TSaga> query,
            ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class =>
            _dispatchRepository.SendQueryAsync(context, query, policy, next);

        public void Probe(ProbeContext context)
        {
            _dispatchRepository.Probe(context);
            _queryRepository.Probe(context);
            _loadRepository.Probe(context);
        }
    }
}
