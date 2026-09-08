using System;
using Azure.Data.Tables;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Supplies the Azure Table client and saga-key strategy used by a repository.</summary>
public interface IAzureTableSagaRepositoryConfigurator
{
    /// <summary>Uses a factory to resolve the Azure Table client.</summary>
    /// <param name="tableClientFactory">The factory invoked to obtain the Azure Table client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tableClientFactory"/> is <see langword="null"/>.</exception>
    void UseTableClientFactory(Func<TableClient> tableClientFactory);

    /// <summary>Uses a service-provider-aware factory to resolve the Azure Table client.</summary>
    /// <param name="tableClientFactory">The factory that resolves an Azure Table client from the registration service provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tableClientFactory"/> is <see langword="null"/>.</exception>
    void UseTableClientFactory(Func<IServiceProvider, TableClient> tableClientFactory);

    /// <summary>Uses the strategy that maps saga identifiers to Azure Table keys.</summary>
    /// <param name="keyFormatter">The saga-key formatting strategy.</param>
    /// <exception cref="ArgumentNullException"><paramref name="keyFormatter"/> is <see langword="null"/>.</exception>
    void UseKeyFormatter(IAzureTableSagaKeyFormatter keyFormatter);
}
