namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using Courier.Contracts;
    using DependencyInjection;
    using JobService;
    using Metadata;
    using Middleware;
    using Transports;


    public class OutboxConsumePipeSpecificationObserver<TContext> :
        IConsumerConfigurationObserver,
        ISagaConfigurationObserver,
        IActivityConfigurationObserver,
        IOutboxOptionsConfigurator
        where TContext : class
    {
        readonly IReceiveEndpointConfigurator _configurator;
        readonly IServiceProvider _serviceProvider;
        readonly ISetScopedConsumeContext _setter;
        readonly string _busKey;

        public OutboxConsumePipeSpecificationObserver(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
            : this(configurator, context, context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)),
                context is IBusRegistrationIdentity identity ? identity.BusKey : "default")
        {
        }

        internal OutboxConsumePipeSpecificationObserver(IReceiveEndpointConfigurator configurator, IServiceProvider serviceProvider,
            ISetScopedConsumeContext setter, string busKey)
        {
            _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _busKey = string.IsNullOrWhiteSpace(busKey) ? throw new ArgumentException("A bus key is required.", nameof(busKey)) : busKey;

            MessageDeliveryLimit = 1;
            MessageDeliveryTimeout = TimeSpan.FromSeconds(30);
        }

        public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
            where TActivity : class, IExecuteActivity<TArguments>
            where TArguments : class
        {
            configurator.RoutingSlip(e => AddScopedFilter<TActivity, RoutingSlip>(e));
        }

        public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
            where TActivity : class, IExecuteActivity<TArguments>
            where TArguments : class
        {
            configurator.RoutingSlip(e => AddScopedFilter<TActivity, RoutingSlip>(e));
        }

        public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
            where TActivity : class, ICompensateActivity<TLog>
            where TLog : class
        {
            configurator.RoutingSlip(e => AddScopedFilter<TActivity, RoutingSlip>(e));
        }

        public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
            where TConsumer : class
        {
        }

        public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
            where TConsumer : class
            where TMessage : class
        {
            if (!(configurator is IConsumerMessageConfigurator<TMessage> messageConfigurator))
                throw new ConfigurationException($"The scoped filter could not be added: {TypeCache<TConsumer>.ShortName} - {TypeCache<TMessage>.ShortName}");

            AddScopedFilter<TConsumer, TMessage>(messageConfigurator);
        }

        public int MessageDeliveryLimit { get; set; } = 1;
        public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
            where TSaga : class, ISaga
        {
        }

        public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, SagaStateMachine<TInstance> stateMachine)
            where TInstance : class, ISaga, SagaStateMachineInstance
        {
        }

        public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
            where TSaga : class, ISaga
            where TMessage : class
        {
            if (!(configurator is ISagaMessageConfigurator<TMessage> messageConfigurator))
                throw new ConfigurationException($"The scoped filter could not be added: {TypeCache<TSaga>.ShortName} - {TypeCache<TMessage>.ShortName}");

            AddScopedFilter<TSaga, TMessage>(messageConfigurator);
        }

        void AddScopedFilter<T, TMessage>(IPipeConfigurator<ConsumeContext<TMessage>> messageConfigurator)
            where T : class
            where TMessage : class
        {
            var scopeProvider = new ConsumeScopeProvider(_serviceProvider, _setter);

            var options = new OutboxConsumeOptions
            {
                ConsumerId = OutboxConsumerIdentity.Create<T, TMessage>(_busKey, _configurator.InputAddress),
                ConsumerType = TypeMetadataCache<T>.ShortName,
                MessageDeliveryLimit = MessageDeliveryLimit,
                MessageDeliveryTimeout = MessageDeliveryTimeout
            };

            var filter = new OutboxConsumeFilter<TContext, TMessage>(scopeProvider, options);

            var specification = new FilterPipeSpecification<ConsumeContext<TMessage>>(filter);

            messageConfigurator.AddPipeSpecification(specification);
        }
    }
}
