using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Creates Azure Table saga repository contexts for message consumption and direct loads.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
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

    /// <summary>Creates a context factory that uses one fixed Azure Table client.</summary>
    /// <param name="tableClient">The Azure Table client used by every repository context.</param>
    /// <param name="factory">The factory that wraps saga instances in consume contexts.</param>
    /// <param name="keyFormatter">The strategy that maps saga identifiers to partition and row keys.</param>
    public AzureTableSagaRepositoryContextFactory(TableClient tableClient,
        ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory,
        ISagaKeyFormatter<TSaga> keyFormatter)
        : this(new FixedTableClientProvider<TSaga>(tableClient), factory, keyFormatter)
    {
    }

    /// <summary>Runs an asynchronous operation against a load-only Azure Table saga context.</summary>
    /// <typeparam name="T">The load result type produced by <paramref name="asyncMethod"/>.</typeparam>
    /// <param name="asyncMethod">The operation to invoke with the load context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The task returned by <paramref name="asyncMethod"/>.</returns>
    public Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(asyncMethod);

        var database = _tableClientProvider.GetTableClient();

        var databaseContext = new AzureTableDatabaseContext<TSaga>(database, _keyFormatter);
        var repositoryContext = new AzureTableLoadSagaRepositoryContext<TSaga>(databaseContext, cancellationToken);

        return asyncMethod(repositoryContext);
    }

    /// <summary>Adds the Azure Table persistence identity to the repository probe.</summary>
    /// <param name="context">The probe context to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Add("persistence", "azuretable");
    }

    /// <summary>Invokes the next saga repository pipe with an Azure Table context for the consumed message.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The message consume context.</param>
    /// <param name="next">The saga repository pipeline to invoke.</param>
    /// <returns>A task that completes when the downstream repository pipeline completes.</returns>
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

    /// <summary>Rejects property-based saga queries because Azure Table correlation is key-based only.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The message consume context.</param>
    /// <param name="query">The unsupported saga query.</param>
    /// <param name="next">The query pipeline that is not invoked.</param>
    /// <returns>This method does not return a task because it always throws.</returns>
    /// <exception cref="NotSupportedException">Azure Table saga persistence does not support query correlation.</exception>
    public async Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        throw new NotSupportedException("Azure Table saga persistence does not support query correlation.");
    }
}
