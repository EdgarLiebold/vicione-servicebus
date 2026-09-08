using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table.Saga;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Creates saga repositories backed by Azure Table Storage.</summary>
public static class AzureTableSagaRepository
{
    /// <summary>Creates a saga repository with caller-defined table resolution and key formatting.</summary>
    /// <typeparam name="TSaga">The saga state persisted by the repository.</typeparam>
    /// <param name="tableClientFactory">The factory that supplies the Azure Table client for each repository context.</param>
    /// <param name="keyFormatter">The strategy that maps saga identifiers to partition and row keys.</param>
    /// <returns>An Azure Table-backed saga repository.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tableClientFactory"/> or <paramref name="keyFormatter"/> is <see langword="null"/>.</exception>
    public static ISagaRepository<TSaga> Create<TSaga>(
        Func<TableClient> tableClientFactory,
        IAzureTableSagaKeyFormatter keyFormatter)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(tableClientFactory);
        ArgumentNullException.ThrowIfNull(keyFormatter);

        var consumeContextFactory = new SagaConsumeContextFactory<IAzureTableSagaStorageContext<TSaga>, TSaga>();

        var tableClientProvider = new DelegateAzureTableClientProvider<TSaga>(tableClientFactory);

        var repositoryContextFactory = new AzureTableSagaRepositoryContextFactory<TSaga>(tableClientProvider, consumeContextFactory, keyFormatter);

        return new SagaRepository<TSaga>(repositoryContextFactory, loadSagaRepositoryContextFactory: repositoryContextFactory);
    }

    /// <summary>Creates a saga repository using the saga type name as a constant partition key.</summary>
    /// <typeparam name="TSaga">The saga state persisted by the repository.</typeparam>
    /// <param name="tableClientFactory">The factory that supplies the Azure Table client for each repository context.</param>
    /// <returns>An Azure Table-backed saga repository.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tableClientFactory"/> is <see langword="null"/>.</exception>
    public static ISagaRepository<TSaga> Create<TSaga>(Func<TableClient> tableClientFactory)
        where TSaga : class, ISaga
    {
        return Create<TSaga>(tableClientFactory, new FixedPartitionSagaKeyFormatter(typeof(TSaga).Name));
    }
}
