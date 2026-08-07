// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
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
