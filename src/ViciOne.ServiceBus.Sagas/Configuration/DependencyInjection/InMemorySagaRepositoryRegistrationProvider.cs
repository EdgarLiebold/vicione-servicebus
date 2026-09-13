namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures in-memory repositories for sagas without an explicit provider.</summary>
internal sealed class InMemorySagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.InMemoryRepository();
    }
}
