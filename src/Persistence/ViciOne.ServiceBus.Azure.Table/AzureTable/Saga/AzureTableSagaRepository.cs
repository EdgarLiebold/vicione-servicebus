using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Provides an azure table saga repository implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public static class AzureTableSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="tableFactory">The table factory value.</param>
    /// <param name="keyFormatter">The key formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public static ISagaRepository<TSaga> Create(Func<TableClient> tableFactory, ISagaKeyFormatter<TSaga> keyFormatter)
    {
        ArgumentNullException.ThrowIfNull(tableFactory);
        ArgumentNullException.ThrowIfNull(keyFormatter);

        var consumeContextFactory = new SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>();

        var tableClientProvider = new DelegateTableClientProvider<TSaga>(tableFactory);

        var repositoryContextFactory = new AzureTableSagaRepositoryContextFactory<TSaga>(tableClientProvider, consumeContextFactory, keyFormatter);

        return new SagaRepository<TSaga>(repositoryContextFactory, loadSagaRepositoryContextFactory: repositoryContextFactory);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="tableFactory">The table factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ISagaRepository<TSaga> Create(Func<TableClient> tableFactory)
    {
        return Create(tableFactory, new ConstPartitionSagaKeyFormatter<TSaga>(typeof(TSaga).Name));
    }
}
