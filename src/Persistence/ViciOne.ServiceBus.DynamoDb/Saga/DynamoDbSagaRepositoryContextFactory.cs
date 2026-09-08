using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

internal sealed class DynamoDbSagaRepositoryContextFactory<TSaga>(
    DynamoDbSagaContextFactory<TSaga> contextFactory,
    ISagaConsumeContextFactory<IDynamoDbSagaStore<TSaga>, TSaga> consumeContextFactory,
    DynamoDbSagaRepositoryOptions<TSaga> options) :
    ISagaRepositoryContextFactory<TSaga>,
    ILoadSagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISagaVersion
{
    readonly DynamoDbSagaContextFactory<TSaga> _contextFactory =
        contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    readonly ISagaConsumeContextFactory<IDynamoDbSagaStore<TSaga>, TSaga> _consumeContextFactory =
        consumeContextFactory ?? throw new ArgumentNullException(nameof(consumeContextFactory));
    readonly DynamoDbSagaRepositoryOptions<TSaga> _options =
        options ?? throw new ArgumentNullException(nameof(options));

    public async Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(asyncMethod);
        var providerContext = _contextFactory.Create();

        var store = new DynamoDbSagaStore<TSaga>(providerContext, _options);
        try
        {
            var repositoryContext = new DynamoDbSagaLoadContext<TSaga>(store, cancellationToken);

            return await asyncMethod(repositoryContext).ConfigureAwait(false);
        }
        finally
        {
            store.Dispose();
        }
    }

    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Add("persistence", "dynamodb");
    }

    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var providerContext = _contextFactory.Create();

        var store = new DynamoDbSagaStore<TSaga>(providerContext, _options);
        try
        {
            var repositoryContext = new DynamoDbSagaRepositoryContext<TSaga, T>(store, context, _consumeContextFactory);

            await next.SendAsync(repositoryContext).ConfigureAwait(false);
        }
        finally
        {
            store.Dispose();
        }
    }

    public Task SendQueryAsync<T>(
        ConsumeContext<T> context,
        ISagaQuery<TSaga> query,
        IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(next);

        return Task.FromException(
            new NotSupportedException("Amazon DynamoDB saga persistence does not support query correlation."));
    }
}
