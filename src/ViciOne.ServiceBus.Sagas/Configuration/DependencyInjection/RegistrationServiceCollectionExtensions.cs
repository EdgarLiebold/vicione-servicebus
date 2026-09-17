using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers the dependency-injection services used by saga repository providers.</summary>
public static class RegistrationServiceCollectionExtensions
{
    static readonly Type[] SagaRepositoryServiceDefinitions =
    [
        typeof(ISagaConsumeContextFactory<,>),
        typeof(ISagaRepositoryContextFactory<>),
        typeof(IQuerySagaRepositoryContextFactory<>),
        typeof(ILoadSagaRepositoryContextFactory<>),
        typeof(IQuerySagaRepository<>),
        typeof(ILoadSagaRepository<>),
        typeof(ISagaRepository<>),
    ];

    /// <summary>Adds the scoped consume-context and repository-context factories for a saga repository provider.</summary>
    /// <typeparam name="TSaga">The saga state managed by the repository.</typeparam>
    /// <typeparam name="TContext">The provider-specific persistence context.</typeparam>
    /// <typeparam name="TConsumeContextFactory">The factory that creates saga consume contexts.</typeparam>
    /// <typeparam name="TRepositoryContextFactory">The factory that executes saga repository operations.</typeparam>
    /// <param name="collection">The dependency-injection service collection.</param>
    /// <remarks>Each call appends one ordered descriptor pair and does not replace an earlier provider registration.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public static void RegisterSagaRepository<TSaga, TContext, TConsumeContextFactory, TRepositoryContextFactory>(this IServiceCollection collection)
        where TSaga : class, ISaga
        where TContext : class
        where TConsumeContextFactory : class, ISagaConsumeContextFactory<TContext, TSaga>
        where TRepositoryContextFactory : class, ISagaRepositoryContextFactory<TSaga>
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.AddScoped<ISagaConsumeContextFactory<TContext, TSaga>, TConsumeContextFactory>();
        collection.AddScoped<ISagaRepositoryContextFactory<TSaga>, TRepositoryContextFactory>();
    }

    /// <summary>Adds the singleton query facade and scoped query-context factory for a saga repository provider.</summary>
    /// <typeparam name="TSaga">The saga state queried by the repository.</typeparam>
    /// <typeparam name="TQueryRepositoryContextFactory">The factory that executes saga queries.</typeparam>
    /// <param name="collection">The dependency-injection service collection.</param>
    /// <remarks>Each call appends one ordered descriptor pair and does not replace an earlier provider registration.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public static void RegisterQuerySagaRepository<TSaga, TQueryRepositoryContextFactory>(this IServiceCollection collection)
        where TSaga : class, ISaga
        where TQueryRepositoryContextFactory : class, IQuerySagaRepositoryContextFactory<TSaga>
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.AddSingleton<IQuerySagaRepository<TSaga>, DependencyInjectionQuerySagaRepository<TSaga>>();
        collection.AddScoped<IQuerySagaRepositoryContextFactory<TSaga>, TQueryRepositoryContextFactory>();
    }

    /// <summary>Adds the singleton load facade and scoped load-context factory for a saga repository provider.</summary>
    /// <typeparam name="TSaga">The saga state loaded by the repository.</typeparam>
    /// <typeparam name="TLoadRepositoryContextFactory">The factory that executes saga loads.</typeparam>
    /// <param name="collection">The dependency-injection service collection.</param>
    /// <remarks>Each call appends one ordered descriptor pair and does not replace an earlier provider registration.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
    public static void RegisterLoadSagaRepository<TSaga, TLoadRepositoryContextFactory>(this IServiceCollection collection)
        where TSaga : class, ISaga
        where TLoadRepositoryContextFactory : class, ILoadSagaRepositoryContextFactory<TSaga>
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.AddSingleton<ILoadSagaRepository<TSaga>, DependencyInjectionLoadSagaRepository<TSaga>>();
        collection.AddScoped<ILoadSagaRepositoryContextFactory<TSaga>, TLoadRepositoryContextFactory>();
    }

    internal static void RemoveSagaRepositories(this IServiceCollection collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        ServiceDescriptor[] descriptors = collection
            .Where(descriptor => IsSagaRepositoryService(descriptor.ServiceType))
            .ToArray();

        foreach (ServiceDescriptor descriptor in descriptors)
            collection.Remove(descriptor);
    }

    static bool IsSagaRepositoryService(Type serviceType)
    {
        Type candidate = serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition() : serviceType;
        return SagaRepositoryServiceDefinitions.Contains(candidate);
    }
}
