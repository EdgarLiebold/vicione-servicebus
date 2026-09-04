using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// The modern query saga repository, which can be used with any storage engine. Leverages the new interfaces for query context.
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public class LoadSagaRepository<TSaga> :
    ILoadSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly ILoadSagaRepositoryContextFactory<TSaga> _repositoryContextFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="repositoryContextFactory">The repository context factory value.</param>
    public LoadSagaRepository(ILoadSagaRepositoryContextFactory<TSaga> repositoryContextFactory)
    {
        _repositoryContextFactory = repositoryContextFactory;
    }

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _repositoryContextFactory.ExecuteAsync(context => context.LoadAsync(correlationId, cancellationToken: cancellationToken), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("loadSagaRepository");

        _repositoryContextFactory.Probe(scope);
    }
}
