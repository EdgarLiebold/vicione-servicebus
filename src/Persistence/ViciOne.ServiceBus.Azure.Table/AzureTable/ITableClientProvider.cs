namespace ViciOne.ServiceBus.AzureTable
{
    using Azure.Data.Tables;


    internal interface ITableClientProvider<in TSaga>
        where TSaga : class, ISaga
    {
        TableClient GetTableClient();
    }
}
