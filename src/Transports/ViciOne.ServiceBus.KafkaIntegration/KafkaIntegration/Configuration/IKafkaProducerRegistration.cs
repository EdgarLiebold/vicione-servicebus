// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IKafkaProducerRegistration :
        IRegistration
    {
        void Register(IKafkaFactoryConfigurator configurator, IRiderRegistrationContext context);
    }
}
