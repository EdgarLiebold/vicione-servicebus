using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Stores and retrieves dependency injection saga data.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DependencyInjectionSagaRepository<TSaga> :
    ISagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaRepositoryContextFactory<TSaga> _repositoryContextFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public DependencyInjectionSagaRepository(IRegistrationContext context)
        : this(new DependencyInjectionSagaRepositoryContextFactory<TSaga>(context))
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="setter">The setter.</param>
    public DependencyInjectionSagaRepository(IServiceProvider serviceProvider, ISetScopedConsumeContext setter)
        : this(new DependencyInjectionSagaRepositoryContextFactory<TSaga>(serviceProvider, setter))
    {
    }

    DependencyInjectionSagaRepository(ISagaRepositoryContextFactory<TSaga> repositoryContextFactory)
    {
        _repositoryContextFactory = repositoryContextFactory;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("dependencyInjectionSagaRepository");

        _repositoryContextFactory.Probe(scope);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="policy">The policy.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync<T>(ConsumeContext<T> context, ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next)
        where T : class
    {
        var correlationId = context.CorrelationId ??
            throw new SagaException("The CorrelationId was not specified", typeof(TSaga), typeof(T));

        return _repositoryContextFactory.SendAsync(context, new SendSagaPipe<TSaga, T>(policy, next, correlationId));
    }

    /// <summary>Sends query.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="query">The query.</param>
    /// <param name="policy">The policy.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, ISagaPolicy<TSaga, T> policy,
        IPipe<SagaConsumeContext<TSaga, T>> next)
        where T : class
    {
        return _repositoryContextFactory.SendQueryAsync(context, query, new SendQuerySagaPipe<TSaga, T>(policy, next));
    }
}
