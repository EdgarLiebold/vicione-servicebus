using System;
using System.Reflection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes outbox consume pipe specification events.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
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

    /// <summary>Reports that activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        ConfigureExecuteActivityMessage(configurator);
    }

    /// <summary>Reports that execute activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        ConfigureExecuteActivityMessage(configurator);
    }

    /// <summary>Reports that compensate activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
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

    /// <summary>Accepts consumer-wide configuration without modifying it.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
    }

    /// <summary>Adds the outbox consume filter to the configured consumer message pipeline.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        if (!(configurator is IConsumerMessageConfigurator<TMessage> messageConfigurator))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Outbox Consume Pipe Specification Observer", "unknown", $"The scoped filter could not be added: {TypeCache<TConsumer>.ShortName} - {TypeCache<TMessage>.ShortName}", "Correct the named configuration before starting the host"));

        AddScopedFilter<TConsumer, TMessage>(messageConfigurator);
    }

    /// <summary>Gets or sets the message delivery limit.</summary>
    public int MessageDeliveryLimit { get; set; } = 1;
    /// <summary>Gets or sets the message delivery timeout.</summary>
    public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Reports that saga has been configured.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class
    {
    }

    /// <summary>Reports that state machine saga has been configured.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
    }

    /// <summary>Reports that saga message has been configured.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
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
            ConsumerType = TypeCache<T>.ShortName,
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
