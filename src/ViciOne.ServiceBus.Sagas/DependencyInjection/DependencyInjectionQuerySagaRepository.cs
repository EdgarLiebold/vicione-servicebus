using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Stores and retrieves dependency injection query saga data.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DependencyInjectionQuerySagaRepository<TSaga> :
    QuerySagaRepository<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public DependencyInjectionQuerySagaRepository(IServiceProvider provider)
        : base(new DependencyInjectionQuerySagaRepositoryContextFactory(
            provider ?? throw new ArgumentNullException(nameof(provider))))
    {
    }


    class DependencyInjectionQuerySagaRepositoryContextFactory :
        IQuerySagaRepositoryContextFactory<TSaga>
    {
        readonly IServiceProvider _serviceProvider;

        public DependencyInjectionQuerySagaRepositoryContextFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Probe(ProbeContext context)
        {
            context.Add("provider", "dependencyInjection");
        }

        public async Task<T> ExecuteAsync<T>(Func<IQuerySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(asyncMethod);

            var serviceScope = _serviceProvider.CreateScope();
            T? result = default;
            Exception? operationFailure = null;

            try
            {
                var factory = serviceScope.ServiceProvider.GetRequiredService<IQuerySagaRepositoryContextFactory<TSaga>>();

                Task<T> execution = factory.ExecuteAsync(asyncMethod, cancellationToken)
                    ?? throw new InvalidOperationException("The scoped saga query context factory returned a null task.");

                result = await execution.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                operationFailure = exception;
            }

            await DependencyInjectionSagaScope.DisposeAsync(
                    serviceScope,
                    operationFailure,
                    "The saga query operation and its dependency injection scope both failed.")
                .ConfigureAwait(false);

            return result!;
        }
    }
}
