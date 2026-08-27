namespace ViciOne.ServiceBus.AzureTable.Saga
{
    using System;
    using Azure.Data.Tables;


    public class DelegateCloudTableProvider<TSaga> :
        ICloudTableProvider<TSaga>
        where TSaga : class, ISaga
    {
        readonly Func<TableClient> _cloudTable;

        public DelegateCloudTableProvider(Func<TableClient> cloudTable)
        {
            ArgumentNullException.ThrowIfNull(cloudTable);
            _cloudTable = cloudTable;
        }

        public TableClient GetCloudTable()
        {
            return _cloudTable()
                ?? throw new InvalidOperationException("The Azure Table client factory returned null.");
        }
    }
}
