using Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

internal interface IAzureTableClientProvider<in TSaga>
    where TSaga : class, ISaga
{
    TableClient GetTableClient();
}
