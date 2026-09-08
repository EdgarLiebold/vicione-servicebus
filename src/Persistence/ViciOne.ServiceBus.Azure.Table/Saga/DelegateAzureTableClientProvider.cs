using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

internal sealed class DelegateAzureTableClientProvider<TSaga> :
    IAzureTableClientProvider<TSaga>
    where TSaga : class, ISaga
{
    readonly Func<TableClient> _tableClientFactory;

    public DelegateAzureTableClientProvider(Func<TableClient> tableClientFactory)
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
