using System;
using Azure.Data.Tables;

namespace ViciOne.ServiceBus.AzureTable.Saga;

internal sealed class FixedTableClientProvider<TSaga> :
    ITableClientProvider<TSaga>
    where TSaga : class, ISaga
{
    readonly TableClient _tableClient;

    public FixedTableClientProvider(TableClient tableClient)
    {
        ArgumentNullException.ThrowIfNull(tableClient);
        _tableClient = tableClient;
    }

    public TableClient GetTableClient()
    {
        return _tableClient;
    }
}
