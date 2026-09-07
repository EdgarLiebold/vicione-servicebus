using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Stores and retrieves dependency injection load saga data.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DependencyInjectionLoadSagaRepository<TSaga> :
    LoadSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
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
