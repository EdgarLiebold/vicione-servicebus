using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a scoped consume pipe specification observer implementation.
/// </summary>
public class ScopedConsumePipeSpecificationObserver :
    IConsumerConfigurationObserver,
    ISagaConfigurationObserver
{
    readonly IRegistrationContext _context;
    readonly Type _filterType;
    readonly CompositeFilter<Type> _messageTypeFilter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filterType">The filter type value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="messageTypeFilter">The message type filter value.</param>
    public ScopedConsumePipeSpecificationObserver(Type filterType, IRegistrationContext context, CompositeFilter<Type> messageTypeFilter)
    {
        _filterType = filterType;
        _context = context;
        _messageTypeFilter = messageTypeFilter;
        // do not create filters for scheduled/outbox messages
        _messageTypeFilter.Excludes.Add(type => type == typeof(SerializedMessageBody));
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
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Scoped Consume Pipe Specification Observer", "unknown", $"The scoped filter could not be added: {TypeCache<TConsumer>.ShortName} - {TypeCache<TMessage>.ShortName}", "Correct the named configuration before starting the host"));

        AddScopedFilter(messageConfigurator);
    }

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
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Scoped Consume Pipe Specification Observer", "unknown", $"The scoped filter could not be added: {TypeCache<TSaga>.ShortName} - {TypeCache<TMessage>.ShortName}", "Correct the named configuration before starting the host"));

        AddScopedFilter(messageConfigurator);
    }

    void AddScopedFilter<TMessage>(IPipeConfigurator<ConsumeContext<TMessage>> messageConfigurator)
        where TMessage : class
    {
        if (!_messageTypeFilter.Matches(typeof(TMessage)))
            return;

        var filterType = _filterType.ImplementsInterface<IFilter<ConsumeContext<TMessage>>>()
            ? _filterType
            : _filterType.MakeGenericType(typeof(TMessage));

        if (!filterType.ImplementsInterface(typeof(IFilter<ConsumeContext<TMessage>>)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Scoped Consume Pipe Specification Observer", "unknown", $"The scoped filter must implement {TypeCache<IFilter<ConsumeContext<TMessage>>>.ShortName} ", "Correct the named configuration before starting the host"));

        var scopeProvider = new ConsumeScopeProvider(_context);

        var scopedFilterType = typeof(ScopedConsumeFilter<,>).MakeGenericType(typeof(TMessage), filterType);

        var filter = (IFilter<ConsumeContext<TMessage>>)(Activator.CreateInstance(scopedFilterType, scopeProvider) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        var specification = new FilterPipeSpecification<ConsumeContext<TMessage>>(filter);

        messageConfigurator.AddPipeSpecification(specification);
    }
}
