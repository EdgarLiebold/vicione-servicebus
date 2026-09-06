using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.AzureTable;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Configures Azure Table persistence for one saga type.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IAzureTableSagaRepositoryConfigurator<TSaga> :
    IAzureTableSagaRepositoryConfigurator
    where TSaga : class, ISaga
{
    /// <summary>Use a factory method to create the key formatter.</summary>
    /// <param name="formatterFactory">The factory that creates the saga key formatter.</param>
    void KeyFormatter(Func<ISagaKeyFormatter<TSaga>> formatterFactory);
}


/// <summary>Supplies the Azure Table client used by a saga repository.</summary>
public interface IAzureTableSagaRepositoryConfigurator
{
    /// <summary>Use a simple factory method to create the Azure Data Tables client.</summary>
    /// <param name="tableClientFactory">The factory invoked to obtain the Azure Table client.</param>
    void TableClientFactory(Func<TableClient> tableClientFactory);

    /// <summary>Use a service-provider-aware factory method to create the Azure Data Tables client.</summary>
    /// <param name="tableClientFactory">The factory that resolves an Azure Table client from the registration service provider.</param>
    void TableClientFactory(Func<IServiceProvider, TableClient> tableClientFactory);
}
