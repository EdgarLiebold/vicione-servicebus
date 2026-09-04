using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureTable.Saga;

public class AzureTableSagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>,
    ILoadSagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly ITableClientProvider<TSaga> _tableClientProvider;
    readonly ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> _factory;
    readonly ISagaKeyFormatter<TSaga> _keyFormatter;

    internal AzureTableSagaRepositoryContextFactory(ITableClientProvider<TSaga> tableClientProvider,
        ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory,
        ISagaKeyFormatter<TSaga> keyFormatter)
    {
        ArgumentNullException.ThrowIfNull(tableClientProvider);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(keyFormatter);

        _tableClientProvider = tableClientProvider;
        _factory = factory;
        _keyFormatter = keyFormatter;
    }

    public AzureTableSagaRepositoryContextFactory(TableClient tableClient,
        ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory,
        ISagaKeyFormatter<TSaga> keyFormatter)
        : this(new FixedTableClientProvider<TSaga>(tableClient), factory, keyFormatter)
    {
    }

    public Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(asyncMethod);

        var database = _tableClientProvider.GetTableClient();

        var databaseContext = new AzureTableDatabaseContext<TSaga>(database, _keyFormatter);
        var repositoryContext = new AzureTableLoadSagaRepositoryContext<TSaga>(databaseContext, cancellationToken);

        return asyncMethod(repositoryContext);
    }

    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Add("persistence", "azuretable");
    }

    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var database = _tableClientProvider.GetTableClient();

        var databaseContext = new AzureTableDatabaseContext<TSaga>(database, _keyFormatter);

        var repositoryContext = new AzureTableSagaRepositoryContext<TSaga, T>(databaseContext, context, _factory);

        await next.SendAsync(repositoryContext).ConfigureAwait(false);
    }

    public async Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        throw new NotImplementedByDesignException("Azure Table repository does not support queries");
    }
}
