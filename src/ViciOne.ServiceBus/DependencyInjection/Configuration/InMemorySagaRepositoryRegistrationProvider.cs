namespace ViciOne.ServiceBus.Configuration
{
    public class InMemorySagaRepositoryRegistrationProvider :
        ISagaRepositoryRegistrationProvider
    {
        public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
            where TSaga : class, ISaga
        {
            configurator.InMemoryRepository();
        }
    }
}
