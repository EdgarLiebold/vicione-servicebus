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

    public LoadSagaRepository(ILoadSagaRepositoryContextFactory<TSaga> repositoryContextFactory)
    {
        _repositoryContextFactory = repositoryContextFactory;
    }

    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _repositoryContextFactory.ExecuteAsync(context => context.LoadAsync(correlationId, cancellationToken: cancellationToken), cancellationToken: cancellationToken);
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("loadSagaRepository");

        _repositoryContextFactory.Probe(scope);
    }
}
