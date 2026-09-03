namespace ViciOne.ServiceBus
{
    using System;
    using ActiveMqTransport;
    using ActiveMqTransport.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;


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
            configurator.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, ActiveMqSendFailureClassifier>());
            configurator.SetBusFactory(new ActiveMqRegistrationBusFactory(configure));
        }
    }
}
