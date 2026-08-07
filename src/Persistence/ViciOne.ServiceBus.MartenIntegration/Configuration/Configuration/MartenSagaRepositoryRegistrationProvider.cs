// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class MartenSagaRepositoryRegistrationProvider :
        ISagaRepositoryRegistrationProvider
    {
        readonly bool _optimisticConcurrency;

        public MartenSagaRepositoryRegistrationProvider(bool optimisticConcurrency = false)
        {
            _optimisticConcurrency = optimisticConcurrency;
        }

        public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
            where TSaga : class, ISaga
        {
            configurator.MartenRepository(schema =>
            {
                if (_optimisticConcurrency)
                    schema.UseOptimisticConcurrency(true);
            });
        }
    }
}
