using System;
using System.Reflection;
using ViciOne.ServiceBus.DependencyInjection;
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
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        ConfigureExecuteActivityMessage(configurator);
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        ConfigureExecuteActivityMessage(configurator);
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        MethodInfo method = typeof(OutboxConsumePipeSpecificationObserver<TContext>)
                .GetMethod(nameof(ConfigureCompensateActivityMessage), BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The {nameof(ConfigureCompensateActivityMessage)} method was not found.");

        method.MakeGenericMethod(typeof(TActivity), typeof(TLog), configurator.MessageType)
            .Invoke(this, [configurator]);
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
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Outbox Consume Pipe Specification Observer", "unknown", $"The scoped filter could not be added: {TypeCache<TConsumer>.ShortName} - {TypeCache<TMessage>.ShortName}", "Correct the named configuration before starting the host"));

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
        where TSaga : class
    {
    }

    /// <summary>
    /// Performs the state machine saga configured operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="stateMachine">The state machine value.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
    }

    /// <summary>
    /// Performs the saga message configured operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class
        where TMessage : class
    {
        if (!(configurator is ISagaMessageConfigurator<TMessage> messageConfigurator))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Outbox Consume Pipe Specification Observer", "unknown", $"The scoped filter could not be added: {TypeCache<TSaga>.ShortName} - {TypeCache<TMessage>.ShortName}", "Correct the named configuration before starting the host"));

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

    void ConfigureExecuteActivityMessage<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        MethodInfo method = typeof(OutboxConsumePipeSpecificationObserver<TContext>)
                .GetMethod(nameof(ConfigureExecuteActivityMessageCore), BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The {nameof(ConfigureExecuteActivityMessageCore)} method was not found.");

        method.MakeGenericMethod(typeof(TActivity), typeof(TArguments), configurator.MessageType)
            .Invoke(this, [configurator]);
    }

    void ConfigureExecuteActivityMessageCore<TActivity, TArguments, TMessage>(
        IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
        where TMessage : class
    {
        configurator.Message<TMessage>(messageConfigurator => AddScopedFilter<TActivity, TMessage>(messageConfigurator));
    }

    void ConfigureCompensateActivityMessage<TActivity, TLog, TMessage>(
        ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
        where TMessage : class
    {
        configurator.Message<TMessage>(messageConfigurator => AddScopedFilter<TActivity, TMessage>(messageConfigurator));
    }
}
