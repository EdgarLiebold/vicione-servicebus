using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies one EF Core repository configuration to saga registrations discovered by type.</summary>
public class EntityFrameworkSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    readonly Action<IEntityFrameworkSagaRepositoryConfigurator>? _configure;

    /// <summary>Initializes a provider with the callback applied to each discovered saga type.</summary>
    /// <param name="configure">The callback that configures each EF Core saga repository.</param>
    public EntityFrameworkSagaRepositoryRegistrationProvider(Action<IEntityFrameworkSagaRepositoryConfigurator>? configure)
    {
        _configure = configure;
    }

    /// <summary>Registers an EF Core repository for the specified saga type.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The saga registration to associate with an EF Core repository.</param>
    public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.EntityFrameworkRepository(r => _configure?.Invoke(r));
    }
}
