using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class MessageSessionSagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaConsumeContextFactory<MessageSessionContext, TSaga> _factory;

    public MessageSessionSagaRepositoryContextFactory(ISagaConsumeContextFactory<MessageSessionContext, TSaga> factory)
    {
        _factory = factory;
    }

    public void Probe(ProbeContext context)
    {
        context.Add("persistence", "azure-service-bus-message-session");
    }

    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        var repositoryContext = new MessageSessionSagaRepositoryContext<TSaga, T>(context, _factory);

        await next.SendAsync(repositoryContext).ConfigureAwait(false);
    }

    public async Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        throw new NotImplementedException(
            $"Query-based saga correlation is not available when using the MessageSession-based saga repository: {TypeCache<TSaga>.ShortName}");
    }
}
