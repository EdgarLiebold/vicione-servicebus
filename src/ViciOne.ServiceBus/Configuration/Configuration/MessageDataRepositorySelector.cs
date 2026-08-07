// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    class MessageDataRepositorySelector :
        IMessageDataRepositorySelector
    {
        public MessageDataRepositorySelector(IBusFactoryConfigurator configurator)
        {
            Configurator = configurator;
        }

        public IBusFactoryConfigurator Configurator { get; }
    }
}
