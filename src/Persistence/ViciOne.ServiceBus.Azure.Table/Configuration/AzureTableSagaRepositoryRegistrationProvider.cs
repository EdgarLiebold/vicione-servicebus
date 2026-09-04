using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an azure table saga repository registration provider implementation.
/// </summary>
public class AzureTableSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    readonly Action<IAzureTableSagaRepositoryConfigurator> _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public AzureTableSagaRepositoryRegistrationProvider(Action<IAzureTableSagaRepositoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configure = configure;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.AzureTableRepository(_configure);
    }
}
