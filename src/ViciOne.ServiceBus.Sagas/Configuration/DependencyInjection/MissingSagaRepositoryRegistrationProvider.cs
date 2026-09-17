namespace ViciOne.ServiceBus.Configuration;

internal sealed class MissingSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);

        throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                "Saga repository",
                TypeCache<TSaga>.ShortName,
                "No repository was configured for the saga.",
                "Configure a repository on the saga registration or select a default saga repository provider"));
    }
}
