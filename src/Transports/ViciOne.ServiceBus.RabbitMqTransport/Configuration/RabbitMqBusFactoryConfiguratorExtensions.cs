namespace ViciOne.ServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using RabbitMqTransport;
    using RabbitMqTransport.Configuration;
    using RabbitMqTransport.Operations;


    public static class RabbitMqBusFactoryConfiguratorExtensions
    {
        /// <summary>
        /// Select RabbitMQ as the transport for the service bus
        /// </summary>
        public static IBusControl CreateUsingRabbitMq(this IBusFactorySelector selector, Action<IRabbitMqBusFactoryConfigurator> configure = null)
        {
            return RabbitMqBusFactory.Create(configure);
        }

        /// <summary>
        /// Configure ViciOne.ServiceBus to use RabbitMQ for the transport.
        /// </summary>
        /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
        /// <param name="configure">The configuration callback for the bus factory</param>
        public static void UsingRabbitMq(this IBusRegistrationConfigurator configurator,
            Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator> configure = null)
        {
            configurator.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, RabbitMqSendFailureClassifier>());
            configurator.TryAddSingleton<IRabbitMqQueueOperations, RabbitMqQueueOperations>();
            configurator.TryAddSingleton(typeof(IRabbitMqQueueOperations<>), typeof(RabbitMqQueueOperations<>));
            configurator.SetBusFactory(new RabbitMqRegistrationBusFactory(configure));
        }
    }
}
