using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a dependency injection load saga repository implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class DependencyInjectionLoadSagaRepository<TSaga> :
    LoadSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public DependencyInjectionLoadSagaRepository(IServiceProvider provider)
        : base(new DependencyInjectionLoadSagaRepositoryContextFactory(provider))
    {
    }


    class DependencyInjectionLoadSagaRepositoryContextFactory :
        ILoadSagaRepositoryContextFactory<TSaga>
    {
        readonly IServiceProvider _serviceProvider;

        public DependencyInjectionLoadSagaRepositoryContextFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var serviceScope = _serviceProvider.CreateScope();

            try
            {
                var factory = serviceScope.ServiceProvider.GetRequiredService<ILoadSagaRepositoryContextFactory<TSaga>>();

                return await factory.ExecuteAsync(asyncMethod, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (serviceScope is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else
                    serviceScope.Dispose();
            }
        }

        public void Probe(ProbeContext context)
        {
            context.Add("provider", "dependencyInjection");
        }
    }
}
