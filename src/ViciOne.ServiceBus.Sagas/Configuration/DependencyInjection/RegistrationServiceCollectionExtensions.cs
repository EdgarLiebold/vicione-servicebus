using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for registration service collection.</summary>
public static class RegistrationServiceCollectionExtensions
{
    /// <summary>Registers saga repository.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
    /// <typeparam name="TConsumeContextFactory">The consume context factory type.</typeparam>
    /// <typeparam name="TRepositoryContextFactory">The repository context factory type.</typeparam>
    /// <param name="collection">The collection.</param>
    public static void RegisterSagaRepository<TSaga, TContext, TConsumeContextFactory, TRepositoryContextFactory>(this IServiceCollection collection)
        where TSaga : class, ISaga
        where TContext : class
        where TConsumeContextFactory : class, ISagaConsumeContextFactory<TContext, TSaga>
        where TRepositoryContextFactory : class, ISagaRepositoryContextFactory<TSaga>
    {
        collection.AddScoped<ISagaConsumeContextFactory<TContext, TSaga>, TConsumeContextFactory>();
        collection.AddScoped<ISagaRepositoryContextFactory<TSaga>, TRepositoryContextFactory>();
    }

    /// <summary>Registers query saga repository.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TQueryRepositoryContextFactory">The query repository context factory type.</typeparam>
    /// <param name="collection">The collection.</param>
    public static void RegisterQuerySagaRepository<TSaga, TQueryRepositoryContextFactory>(this IServiceCollection collection)
        where TSaga : class, ISaga
        where TQueryRepositoryContextFactory : class, IQuerySagaRepositoryContextFactory<TSaga>
    {
        collection.AddSingleton<IQuerySagaRepository<TSaga>, DependencyInjectionQuerySagaRepository<TSaga>>();
        collection.AddScoped<IQuerySagaRepositoryContextFactory<TSaga>, TQueryRepositoryContextFactory>();
    }

    /// <summary>Registers load saga repository.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TLoadRepositoryContextFactory">The load repository context factory type.</typeparam>
    /// <param name="collection">The collection.</param>
    public static void RegisterLoadSagaRepository<TSaga, TLoadRepositoryContextFactory>(this IServiceCollection collection)
        where TSaga : class, ISaga
        where TLoadRepositoryContextFactory : class, ILoadSagaRepositoryContextFactory<TSaga>
    {
        collection.AddSingleton<ILoadSagaRepository<TSaga>, DependencyInjectionLoadSagaRepository<TSaga>>();
        collection.AddScoped<ILoadSagaRepositoryContextFactory<TSaga>, TLoadRepositoryContextFactory>();
    }

    internal static void RemoveSagaRepositories(this IServiceCollection collection)
    {
        collection.RemoveAll(typeof(ISagaConsumeContextFactory<,>));
        collection.RemoveAll(typeof(ISagaRepositoryContextFactory<>));
        collection.RemoveAll(typeof(IQuerySagaRepositoryContextFactory<>));
        collection.RemoveAll(typeof(ILoadSagaRepositoryContextFactory<>));

        collection.RemoveAll(typeof(IQuerySagaRepository<>));
        collection.RemoveAll(typeof(ILoadSagaRepository<>));
        collection.RemoveAll(typeof(ISagaRepository<>));
    }
}
