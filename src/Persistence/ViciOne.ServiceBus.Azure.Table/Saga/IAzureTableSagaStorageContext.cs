using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

internal interface IAzureTableSagaStorageContext<TSaga>
    where TSaga : class, ISaga
{
    IAzureTableSagaKeyFormatter Formatter { get; }

    TableClient Table { get; }

    IAzureTableEntityConverter<TSaga> Converter { get; }

    (string partitionKey, string rowKey) Format(Guid correlationId);
}
