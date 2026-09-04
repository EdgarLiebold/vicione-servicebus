using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Provides an azure table saga repository context factory implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="tableClient">The table client value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="keyFormatter">The key formatter value.</param>
    public AzureTableSagaRepositoryContextFactory(TableClient tableClient,
        ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory,
        ISagaKeyFormatter<TSaga> keyFormatter)
        : this(new FixedTableClientProvider<TSaga>(tableClient), factory, keyFormatter)
    {
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="asyncMethod">The async method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(asyncMethod);

        var database = _tableClientProvider.GetTableClient();

        var databaseContext = new AzureTableDatabaseContext<TSaga>(database, _keyFormatter);
        var repositoryContext = new AzureTableLoadSagaRepositoryContext<TSaga>(databaseContext, cancellationToken);

        return asyncMethod(repositoryContext);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Add("persistence", "azuretable");
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
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var database = _tableClientProvider.GetTableClient();

        var databaseContext = new AzureTableDatabaseContext<TSaga>(database, _keyFormatter);

        var repositoryContext = new AzureTableSagaRepositoryContext<TSaga, T>(databaseContext, context, _factory);

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
        throw new NotImplementedByDesignException("Azure Table repository does not support queries");
    }
}
