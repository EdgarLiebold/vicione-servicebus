namespace ViciOne.ServiceBus.AzureTable.Saga
{
    using System;
    using Azure.Data.Tables;


    internal sealed class DelegateTableClientProvider<TSaga> :
        ITableClientProvider<TSaga>
        where TSaga : class, ISaga
    {
        readonly Func<TableClient> _tableClientFactory;

        public DelegateTableClientProvider(Func<TableClient> tableClientFactory)
        {
            ArgumentNullException.ThrowIfNull(tableClientFactory);
            _tableClientFactory = tableClientFactory;
        }

        public TableClient GetTableClient()
        {
            return _tableClientFactory()
                ?? throw new InvalidOperationException("The Azure Table client factory returned null.");
        }
    }
}
