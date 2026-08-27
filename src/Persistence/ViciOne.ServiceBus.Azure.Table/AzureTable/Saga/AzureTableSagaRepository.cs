namespace ViciOne.ServiceBus.AzureTable.Saga
{
    using System;
    using Azure.Data.Tables;
    using ViciOne.ServiceBus.Saga;


    public static class AzureTableSagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        public static ISagaRepository<TSaga> Create(Func<TableClient> tableFactory, ISagaKeyFormatter<TSaga> keyFormatter)
        {
            ArgumentNullException.ThrowIfNull(tableFactory);
            ArgumentNullException.ThrowIfNull(keyFormatter);

            var consumeContextFactory = new SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>();

            var tableClientProvider = new DelegateTableClientProvider<TSaga>(tableFactory);

            var repositoryContextFactory = new AzureTableSagaRepositoryContextFactory<TSaga>(tableClientProvider, consumeContextFactory, keyFormatter);

            return new SagaRepository<TSaga>(repositoryContextFactory, loadSagaRepositoryContextFactory: repositoryContextFactory);
        }

        public static ISagaRepository<TSaga> Create(Func<TableClient> tableFactory)
        {
            return Create(tableFactory, new ConstPartitionSagaKeyFormatter<TSaga>(typeof(TSaga).Name));
        }
    }
}
