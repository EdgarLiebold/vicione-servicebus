using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Creates saga repository contexts backed by the active Azure Service Bus session.</summary>
/// <typeparam name="TSaga">The session-backed saga state type.</typeparam>
public class MessageSessionSagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaConsumeContextFactory<MessageSessionContext, TSaga> _factory;

    /// <summary>Creates a repository-context factory.</summary>
    /// <param name="factory">The factory that creates saga consume contexts.</param>
    public MessageSessionSagaRepositoryContextFactory(ISagaConsumeContextFactory<MessageSessionContext, TSaga> factory)
    {
        _factory = factory;
    }

    /// <summary>Marks the repository probe as Azure Service Bus session persistence.</summary>
    /// <param name="context">The probe receiving persistence metadata.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("persistence", "azure-service-bus-message-session");
    }

    /// <summary>Creates a session-backed repository context and sends it to the next pipeline stage.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The typed consume context containing session metadata.</param>
    /// <param name="next">The repository pipeline stage to invoke.</param>
    /// <returns>The continuation task for the session-backed saga repository context.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<ISagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        var repositoryContext = new MessageSessionSagaRepositoryContext<TSaga, T>(context, _factory);

        await next.SendAsync(repositoryContext).ConfigureAwait(false);
    }

    /// <summary>Evaluates a saga query against the single state stored in the active session.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The typed consume context containing session metadata.</param>
    /// <param name="query">The predicate applied to the current session state.</param>
    /// <param name="next">The query pipeline stage receiving zero or one matching state.</param>
    /// <returns>The continuation task after the current session state has been filtered by <paramref name="query"/>.</returns>
    public async Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<ISagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(next);

        var repositoryContext = new MessageSessionSagaRepositoryContext<TSaga, T>(context, _factory);
        TSaga? current = await repositoryContext.ReadCurrentAsync(context.CancellationToken).ConfigureAwait(false);
        TSaga[] matches = current is not null && query.GetFilter()(current) ? [current] : [];
        var queryContext = new LoadedSagaRepositoryQueryContext<TSaga, T>(repositoryContext, matches);

        await next.SendAsync(queryContext).ConfigureAwait(false);
    }
}
