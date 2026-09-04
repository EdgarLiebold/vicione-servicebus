using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an entity framework saga repository registration provider implementation.
/// </summary>
public class EntityFrameworkSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    readonly Action<IEntityFrameworkSagaRepositoryConfigurator>? _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public EntityFrameworkSagaRepositoryRegistrationProvider(Action<IEntityFrameworkSagaRepositoryConfigurator>? configure)
    {
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
        configurator.EntityFrameworkRepository(r => _configure?.Invoke(r));
    }
}
