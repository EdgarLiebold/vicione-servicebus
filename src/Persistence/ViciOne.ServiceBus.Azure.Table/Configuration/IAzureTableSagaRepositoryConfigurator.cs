using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.AzureTable;

namespace ViciOne.ServiceBus;

public interface IAzureTableSagaRepositoryConfigurator<TSaga> :
    IAzureTableSagaRepositoryConfigurator
    where TSaga : class, ISaga
{
    /// <summary>
    /// Use a factory method to create the key formatter
    /// </summary>
    /// <param name="formatterFactory"></param>
    void KeyFormatter(Func<ISagaKeyFormatter<TSaga>> formatterFactory);
}


public interface IAzureTableSagaRepositoryConfigurator
{
    /// <summary>
    /// Use a simple factory method to create the Azure Data Tables client.
    /// </summary>
    /// <param name="tableClientFactory">The table-client factory.</param>
    void TableClientFactory(Func<TableClient> tableClientFactory);

    /// <summary>
    /// Use a service-provider-aware factory method to create the Azure Data Tables client.
    /// </summary>
    /// <param name="tableClientFactory">The table-client factory.</param>
    void TableClientFactory(Func<IServiceProvider, TableClient> tableClientFactory);
}
