namespace ViciOne.ServiceBus.AzureTable.Saga
{
    using System;
    using Azure.Data.Tables;


    public class ConstCloudTableProvider<TSaga> :
        ICloudTableProvider<TSaga>
        where TSaga : class, ISaga
    {
        readonly TableClient _cloudTable;

        public ConstCloudTableProvider(TableClient cloudTable)
        {
            ArgumentNullException.ThrowIfNull(cloudTable);
            _cloudTable = cloudTable;
        }

        public TableClient GetCloudTable()
        {
            return _cloudTable;
        }
    }
}
