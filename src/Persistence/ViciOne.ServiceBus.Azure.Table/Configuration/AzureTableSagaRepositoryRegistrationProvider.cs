using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies one Azure Table repository configuration to saga types registered at runtime.</summary>
public class AzureTableSagaRepositoryRegistrationProvider :
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
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The saga registration to update.</param>
    public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.AzureTableRepository(_configure);
    }
}
