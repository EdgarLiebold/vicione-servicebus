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
            AddSharedServices(configurator);
            configurator.TryAddSingleton<IDurableSendDispatcher<IBus>, RabbitMqDurableSendDispatcher<IBus>>();
            configurator.SetBusFactory(new RabbitMqRegistrationBusFactory(configure));
        }

        /// <summary>
        /// Configure a typed ViciOne.ServiceBus instance to use RabbitMQ for the transport.
        /// </summary>
        /// <typeparam name="TBus">The typed bus contract that owns the transport and Durable Sender.</typeparam>
        /// <param name="configurator">The typed registration configurator.</param>
        /// <param name="configure">The configuration callback for the bus factory.</param>
        public static void UsingRabbitMq<TBus>(this IBusRegistrationConfigurator<TBus> configurator,
            Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator> configure = null)
            where TBus : class, IBus
        {
            AddSharedServices(configurator);
            configurator.TryAddSingleton<IDurableSendDispatcher<TBus>, RabbitMqDurableSendDispatcher<TBus>>();
            configurator.SetBusFactory(new RabbitMqRegistrationBusFactory(configure));
        }

        static void AddSharedServices(IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportSendFailureClassifier, RabbitMqSendFailureClassifier>());
            services.TryAddSingleton<IRabbitMqQueueOperations, RabbitMqQueueOperations>();
            services.TryAddSingleton(typeof(IRabbitMqQueueOperations<>), typeof(RabbitMqQueueOperations<>));
        }
    }
}
