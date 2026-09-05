using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides dependency-injection registration for in-memory saga repositories.
/// </summary>
public static class InMemorySagaRepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory saga repository for the specified saga type.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="services">The service collection.</param>
    public static void RegisterInMemorySagaRepository<TSaga>(this IServiceCollection services)
        where TSaga : class, ISaga
    {
        services.TryAddSingleton(new IndexedSagaDictionary<TSaga>());
        services.RegisterLoadSagaRepository<TSaga, InMemorySagaRepositoryContextFactory<TSaga>>();
        services.RegisterQuerySagaRepository<TSaga, InMemorySagaRepositoryContextFactory<TSaga>>();
        services.RegisterSagaRepository<TSaga, IndexedSagaDictionary<TSaga>, InMemorySagaConsumeContextFactory<TSaga>,
            InMemorySagaRepositoryContextFactory<TSaga>>();
    }
}
