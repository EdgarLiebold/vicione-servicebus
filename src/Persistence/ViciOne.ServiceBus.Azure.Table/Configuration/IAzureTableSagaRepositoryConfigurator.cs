using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.AzureTable;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>
/// Defines the contract for azure table saga repository configurator.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
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


/// <summary>
/// Defines the contract for azure table saga repository configurator.
/// </summary>
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
