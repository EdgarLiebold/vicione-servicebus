using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Attaches a dependency-injection-scoped filter to selected consumer and saga message pipelines.</summary>
internal sealed class ScopedConsumePipeSpecificationObserver :
    IConsumerConfigurationObserver,
    ISagaConfigurationObserver
{
    readonly IRegistrationContext _context;
    readonly Type _filterType;
    readonly CompositeFilter<Type> _messageTypeFilter;

    /// <summary>Creates an observer for one filter implementation and message-selection policy.</summary>
    /// <param name="filterType">The closed filter or open-generic filter definition.</param>
    /// <param name="context">The registration context used to create message scopes.</param>
    /// <param name="messageTypeFilter">The policy that selects message contracts.</param>
    public ScopedConsumePipeSpecificationObserver(Type filterType, IRegistrationContext context, CompositeFilter<Type> messageTypeFilter)
    {
        _filterType = filterType ?? throw new ArgumentNullException(nameof(filterType));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _messageTypeFilter = messageTypeFilter ?? throw new ArgumentNullException(nameof(messageTypeFilter));
        // Serialized scheduler and outbox envelopes bypass application message filters.
        _messageTypeFilter.Excludes.Add(type => type == typeof(SerializedTransportMessage));
    }

    /// <summary>Leaves the consumer-level notification unchanged because filters are attached per message.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="configurator">The configured consumer.</param>
    public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
        where TConsumer : class
    {
        // A consumer can handle multiple message contracts; the message callback owns filter placement.
    }

    /// <summary>Attaches the scoped filter to a configured consumer-message pipeline.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="configurator">The configured consumer-message pipeline.</param>
    public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
        where TConsumer : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (!(configurator is IConsumerMessageConfigurator<TMessage> messageConfigurator))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Scoped Consume Pipe Specification Observer", "unknown", $"The scoped filter could not be added: {TypeCache<TConsumer>.ShortName} - {TypeCache<TMessage>.ShortName}", "Correct the named configuration before starting the host"));

        AddScopedFilter(messageConfigurator);
    }

    /// <summary>Leaves the saga-level notification unchanged because filters are attached per message.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configured saga.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class
    {
        // A saga can handle multiple message contracts; the message callback owns filter placement.
    }

    /// <summary>Leaves state-machine metadata unchanged because its message pipelines are observed separately.</summary>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="configurator">The configured saga.</param>
    /// <param name="stateMachine">The state-machine definition.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
        // State-machine metadata does not identify an individual message pipeline.
    }

    /// <summary>Attaches the scoped filter to a configured saga-message pipeline.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="configurator">The configured saga-message pipeline.</param>
    public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

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
