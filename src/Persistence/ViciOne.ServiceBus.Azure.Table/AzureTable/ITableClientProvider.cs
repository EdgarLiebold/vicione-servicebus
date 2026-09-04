using Azure.Data.Tables;

namespace ViciOne.ServiceBus.AzureTable;

internal interface ITableClientProvider<in TSaga>
    where TSaga : class, ISaga
{
    TableClient GetTableClient();
}
