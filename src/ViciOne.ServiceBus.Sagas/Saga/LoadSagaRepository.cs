using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Loads saga state by correlation identifier through a storage-specific context.</summary>
/// <typeparam name="TSaga">The saga state loaded by the repository.</typeparam>
public class LoadSagaRepository<TSaga> :
    ILoadSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly ILoadSagaRepositoryContextFactory<TSaga> _repositoryContextFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repositoryContextFactory">The repository context factory.</param>
    public LoadSagaRepository(ILoadSagaRepositoryContextFactory<TSaga> repositoryContextFactory)
    {
        _repositoryContextFactory = repositoryContextFactory
            ?? throw new ArgumentNullException(nameof(repositoryContextFactory));
    }

    /// <summary>Loads the requested state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the load outcome.</returns>
    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _repositoryContextFactory.ExecuteAsync(
                context => context.LoadAsync(correlationId, cancellationToken)
                    ?? Task.FromException<TSaga?>(new InvalidOperationException("The saga load context returned a null task.")),
                cancellationToken)
            ?? Task.FromException<TSaga?>(new InvalidOperationException("The saga load context factory returned a null task."));
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("loadSagaRepository");

        _repositoryContextFactory.Probe(scope);
    }
}
