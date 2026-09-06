namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides saga repository registration services.</summary>
public interface ISagaRepositoryRegistrationProvider
{
    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga;
}
