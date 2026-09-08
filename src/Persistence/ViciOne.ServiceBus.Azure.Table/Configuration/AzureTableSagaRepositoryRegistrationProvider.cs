using System;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Azure.Table.Configuration;

/// <summary>Applies one Azure Table repository configuration to saga types discovered during registration.</summary>
internal sealed class AzureTableSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    readonly Action<IAzureTableSagaRepositoryConfigurator> _configure;

    /// <summary>Creates a provider from the configuration callback applied to each saga type.</summary>
    /// <param name="configure">The callback that supplies the Azure Table client.</param>
    public AzureTableSagaRepositoryRegistrationProvider(Action<IAzureTableSagaRepositoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configure = configure;
    }

    /// <summary>Registers an Azure Table repository for the specified saga type.</summary>
    /// <typeparam name="TSaga">The saga state receiving the repository registration.</typeparam>
    /// <param name="configurator">The saga registration to update.</param>
    public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.UseAzureTable(repository => _configure(repository));
    }
}
