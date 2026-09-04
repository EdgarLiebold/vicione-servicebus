using System;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an outbox consume pipe specification observer implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
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

    /// <summary>
    /// Performs the activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        configurator.RoutingSlip(e => AddScopedFilter<TActivity, RoutingSlip>(e));
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        configurator.RoutingSlip(e => AddScopedFilter<TActivity, RoutingSlip>(e));
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        configurator.RoutingSlip(e => AddScopedFilter<TActivity, RoutingSlip>(e));
    }

    /// <summary>
    /// Consumes r configured.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
    }

    /// <summary>
    /// Consumes r message configured.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        if (!(configurator is IConsumerMessageConfigurator<TMessage> messageConfigurator))
            throw new ConfigurationException($"The scoped filter could not be added: {TypeCache<TConsumer>.ShortName} - {TypeCache<TMessage>.ShortName}");

        AddScopedFilter<TConsumer, TMessage>(messageConfigurator);
    }

    /// <summary>
    /// Gets or sets the message delivery limit value.
    /// </summary>
    public int MessageDeliveryLimit { get; set; } = 1;
    /// <summary>
    /// Gets or sets the message delivery timeout value.
    /// </summary>
    public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Performs the saga configured operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
    }

    /// <summary>
    /// Performs the state machine saga configured operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="stateMachine">The state machine value.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, SagaStateMachine<TInstance> stateMachine)
        where TInstance : class, ISaga, SagaStateMachineInstance
    {
    }

    /// <summary>
    /// Performs the saga message configured operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
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
