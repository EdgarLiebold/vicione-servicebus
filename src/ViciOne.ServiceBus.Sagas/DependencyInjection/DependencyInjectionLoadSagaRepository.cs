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
        : base(new DependencyInjectionLoadSagaRepositoryContextFactory(
            provider ?? throw new ArgumentNullException(nameof(provider))))
    {
    }


    class DependencyInjectionLoadSagaRepositoryContextFactory :
        ILoadSagaRepositoryContextFactory<TSaga>
    {
        readonly IServiceProvider _serviceProvider;

        public DependencyInjectionLoadSagaRepositoryContextFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<T?> ExecuteAsync<T>(Func<ILoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod,
            CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(asyncMethod);

            var serviceScope = _serviceProvider.CreateScope();
            T? result = default;
            Exception? operationFailure = null;

            try
            {
                var factory = serviceScope.ServiceProvider.GetRequiredService<ILoadSagaRepositoryContextFactory<TSaga>>();

                Task<T?> execution = factory.ExecuteAsync(asyncMethod, cancellationToken)
                    ?? throw new InvalidOperationException("The scoped saga load context factory returned a null task.");

                result = await execution.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                operationFailure = exception;
            }

            await DependencyInjectionSagaScope.DisposeAsync(
                    serviceScope,
                    operationFailure,
                    "The saga load operation and its dependency injection scope both failed.")
                .ConfigureAwait(false);

            return result;
        }

        public void Probe(ProbeContext context)
        {
            context.Add("provider", "dependencyInjection");
        }
    }
}
