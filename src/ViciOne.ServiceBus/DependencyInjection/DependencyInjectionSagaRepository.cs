using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a dependency injection saga repository implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class DependencyInjectionSagaRepository<TSaga> :
    ISagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaRepositoryContextFactory<TSaga> _repositoryContextFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public DependencyInjectionSagaRepository(IRegistrationContext context)
        : this(new DependencyInjectionSagaRepositoryContextFactory<TSaga>(context))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serviceProvider">The service provider value.</param>
    /// <param name="setter">The setter value.</param>
    public DependencyInjectionSagaRepository(IServiceProvider serviceProvider, ISetScopedConsumeContext setter)
        : this(new DependencyInjectionSagaRepositoryContextFactory<TSaga>(serviceProvider, setter))
    {
    }

    DependencyInjectionSagaRepository(ISagaRepositoryContextFactory<TSaga> repositoryContextFactory)
    {
        _repositoryContextFactory = repositoryContextFactory;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("dependencyInjectionSagaRepository");

        _repositoryContextFactory.Probe(scope);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="policy">The policy value.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(ConsumeContext<T> context, ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next)
        where T : class
    {
        var correlationId = context.CorrelationId ??
            throw new SagaException("The CorrelationId was not specified", typeof(TSaga), typeof(T));

        return _repositoryContextFactory.SendAsync(context, new SendSagaPipe<TSaga, T>(policy, next, correlationId));
    }

    /// <summary>
    /// Sends query.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="query">The query value.</param>
    /// <param name="policy">The policy value.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, ISagaPolicy<TSaga, T> policy,
        IPipe<SagaConsumeContext<TSaga, T>> next)
        where T : class
    {
        return _repositoryContextFactory.SendQueryAsync(context, query, new SendQuerySagaPipe<TSaga, T>(policy, next));
    }
}
