// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class DefaultSagaDefinition<TSaga> :
        SagaDefinition<TSaga>
        where TSaga : class, ISaga
    {
    }
}
