using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

internal sealed class FixedAzureTableClientProvider<TSaga> :
    IAzureTableClientProvider<TSaga>
    where TSaga : class, ISaga
{
    readonly TableClient _tableClient;

    public FixedAzureTableClientProvider(TableClient tableClient)
    {
        ArgumentNullException.ThrowIfNull(tableClient);
        _tableClient = tableClient;
    }

    public TableClient GetTableClient()
    {
        return _tableClient;
    }
}
