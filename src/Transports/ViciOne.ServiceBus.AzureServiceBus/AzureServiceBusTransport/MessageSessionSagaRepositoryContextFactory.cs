using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a message session saga repository context factory implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class MessageSessionSagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaConsumeContextFactory<MessageSessionContext, TSaga> _factory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    public MessageSessionSagaRepositoryContextFactory(ISagaConsumeContextFactory<MessageSessionContext, TSaga> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("persistence", "azure-service-bus-message-session");
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        var repositoryContext = new MessageSessionSagaRepositoryContext<TSaga, T>(context, _factory);

        await next.SendAsync(repositoryContext).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends query.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="query">The query value.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
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
