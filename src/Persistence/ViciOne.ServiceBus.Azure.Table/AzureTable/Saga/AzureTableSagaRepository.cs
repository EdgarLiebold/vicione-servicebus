using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Creates saga repositories backed by Azure Table Storage.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public static class AzureTableSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates a saga repository with caller-defined table resolution and key formatting.</summary>
    /// <param name="tableFactory">The factory that supplies the Azure Table client for each repository context.</param>
    /// <param name="keyFormatter">The strategy that maps saga identifiers to partition and row keys.</param>
    /// <returns>An Azure Table-backed saga repository.</returns>
    public static ISagaRepository<TSaga> Create(Func<TableClient> tableFactory, ISagaKeyFormatter<TSaga> keyFormatter)
    {
        ArgumentNullException.ThrowIfNull(tableFactory);
        ArgumentNullException.ThrowIfNull(keyFormatter);

        var consumeContextFactory = new SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>();

        var tableClientProvider = new DelegateTableClientProvider<TSaga>(tableFactory);

        var repositoryContextFactory = new AzureTableSagaRepositoryContextFactory<TSaga>(tableClientProvider, consumeContextFactory, keyFormatter);

        return new SagaRepository<TSaga>(repositoryContextFactory, loadSagaRepositoryContextFactory: repositoryContextFactory);
    }

    /// <summary>Creates a saga repository using the saga type name as a constant partition key.</summary>
    /// <param name="tableFactory">The factory that supplies the Azure Table client for each repository context.</param>
    /// <returns>An Azure Table-backed saga repository.</returns>
    public static ISagaRepository<TSaga> Create(Func<TableClient> tableFactory)
    {
        return Create(tableFactory, new ConstPartitionSagaKeyFormatter<TSaga>(typeof(TSaga).Name));
    }
}
