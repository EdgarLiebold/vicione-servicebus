namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides in memory saga repository registration services.</summary>
public class InMemorySagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.InMemoryRepository();
    }
}
