using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Attaches one dependency-injection scope to each consumed message or routing-slip activity.</summary>
internal sealed class MessageScopeConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly IServiceProvider _serviceProvider;
    readonly ISetScopedConsumeContext _setScopedConsumeContext;

    /// <summary>Creates an observer that uses the registration context's scoped consume-context accessor.</summary>
    /// <param name="receiveEndpointConfigurator">The consume pipeline whose messages require scopes.</param>
    /// <param name="context">The registration context that owns scoped consume-context state.</param>
    public MessageScopeConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator, IRegistrationContext context)
        : this(receiveEndpointConfigurator, RequireContext(context), RequireScopedContextAccessor(context))
    {
    }

    /// <summary>Creates an observer that establishes message scopes through the supplied services and context accessor.</summary>
    /// <param name="receiveEndpointConfigurator">The consume pipeline whose messages require scopes.</param>
    /// <param name="serviceProvider">The root provider used to create message scopes.</param>
    /// <param name="setScopedConsumeContext">The accessor that installs and restores ambient consume contexts.</param>
    public MessageScopeConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator, IServiceProvider serviceProvider,
        ISetScopedConsumeContext setScopedConsumeContext)
        : base(RequireDependencies(receiveEndpointConfigurator, serviceProvider, setScopedConsumeContext))
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _setScopedConsumeContext = setScopedConsumeContext ?? throw new ArgumentNullException(nameof(setScopedConsumeContext));

        Connect(this);
    }

    /// <summary>Adds a message scope filter to a configured consume pipeline.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="configurator">The configured message pipeline.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var scopeProvider = new ConsumeScopeProvider(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeMessageFilter<TMessage>(scopeProvider);
        var specification = new FilterPipeSpecification<ConsumeContext<TMessage>>(scopeFilter);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds a message scope filter to a configured batch-consumer pipeline.</summary>
    /// <typeparam name="TConsumer">The batch consumer implementation.</typeparam>
    /// <typeparam name="TMessage">The message contract contained in the batch.</typeparam>
    /// <param name="configurator">The configured batch-message pipeline.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, IMessageBatch<TMessage>> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (!(configurator is IConsumerMessageSpecification<TConsumer, IMessageBatch<TMessage>> consumerSpecification))
            throw new ArgumentException("The configurator must be a consumer specification");

        var scopeProvider = new ConsumeScopeProvider(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeMessageFilter<IMessageBatch<TMessage>>(scopeProvider);
        var specification = new FilterPipeSpecification<ConsumeContext<IMessageBatch<TMessage>>>(scopeFilter);

        consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Adds an execution scope filter to a configured activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execution-argument contract.</typeparam>
    /// <param name="configurator">The configured execution pipeline.</param>
    /// <param name="compensateAddress">The companion compensation endpoint address.</param>
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var scopeProvider = new ExecuteScopeProvider<TArguments>(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeExecuteFilter<TArguments>(scopeProvider);
        var specification = new FilterPipeSpecification<ExecuteContext<TArguments>>(scopeFilter);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Adds an execution scope filter to a configured execute-activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execution-argument contract.</typeparam>
    /// <param name="configurator">The configured execution pipeline.</param>
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var scopeProvider = new ExecuteScopeProvider<TArguments>(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeExecuteFilter<TArguments>(scopeProvider);
        var specification = new FilterPipeSpecification<ExecuteContext<TArguments>>(scopeFilter);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Adds a compensation scope filter to a configured compensate-activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TLog">The compensation-log contract.</typeparam>
    /// <param name="configurator">The configured compensation pipeline.</param>
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var scopeProvider = new CompensateScopeProvider<TLog>(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeCompensateFilter<TLog>(scopeProvider);
        var specification = new FilterPipeSpecification<CompensateContext<TLog>>(scopeFilter);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }

    static IRegistrationContext RequireContext(IRegistrationContext? context)
    {
        return context ?? throw new ArgumentNullException(nameof(context));
    }

    static IConsumePipeConfigurator RequireDependencies(IConsumePipeConfigurator? receiveEndpointConfigurator,
        IServiceProvider? serviceProvider, ISetScopedConsumeContext? setScopedConsumeContext)
    {
        ArgumentNullException.ThrowIfNull(receiveEndpointConfigurator);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(setScopedConsumeContext);

        return receiveEndpointConfigurator;
    }

    static ISetScopedConsumeContext RequireScopedContextAccessor(IRegistrationContext? context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context as ISetScopedConsumeContext
            ?? throw new ArgumentException("The registration context does not carry a scoped consume context setter", nameof(context));
    }
}
