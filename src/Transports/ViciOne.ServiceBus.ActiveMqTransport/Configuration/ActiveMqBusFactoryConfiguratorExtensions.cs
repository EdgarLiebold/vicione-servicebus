// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using ActiveMqTransport.Configuration;


    public static class ActiveMqBusFactoryConfiguratorExtensions
    {
        /// <summary>
        /// Select ActiveMQ as the transport for the service bus
        /// </summary>
        public static IBusControl CreateUsingActiveMq(this IBusFactorySelector selector, Action<IActiveMqBusFactoryConfigurator> configure)
        {
            return ActiveMqBusFactory.Create(configure);
        }

        /// <summary>
        /// Configure ViciOne.ServiceBus to use ActiveMQ for the transport.
        /// </summary>
        /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
        /// <param name="configure">The configuration callback for the bus factory</param>
        public static void UsingActiveMq(this IBusRegistrationConfigurator configurator,
            Action<IBusRegistrationContext, IActiveMqBusFactoryConfigurator> configure = null)
        {
            configurator.SetBusFactory(new ActiveMqRegistrationBusFactory(configure));
        }
    }
}
