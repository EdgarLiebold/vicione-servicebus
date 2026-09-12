using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes message scope configuration events.</summary>
public class MessageScopeConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly IServiceProvider _serviceProvider;
    readonly ISetScopedConsumeContext _setScopedConsumeContext;

    /// <summary>Creates an observer that uses the registration context's scoped consume-context accessor.</summary>
    /// <param name="receiveEndpointConfigurator">The receive endpoint configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    public MessageScopeConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator, IRegistrationContext context)
        : this(receiveEndpointConfigurator, context,
            context as ISetScopedConsumeContext ?? throw new ArgumentException(
                "The registration context does not carry a scoped consume context setter", nameof(context)))
    {
    }

    /// <summary>Creates an observer that establishes message scopes through the supplied services and context accessor.</summary>
    /// <param name="receiveEndpointConfigurator">The receive endpoint configurator.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="setScopedConsumeContext">The set scoped consume context.</param>
    public MessageScopeConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator, IServiceProvider serviceProvider,
        ISetScopedConsumeContext setScopedConsumeContext)
        : base(receiveEndpointConfigurator)
    {
        _serviceProvider = serviceProvider;
        _setScopedConsumeContext = setScopedConsumeContext;

        Connect(this);
    }

    /// <summary>Adds a message scope filter to a configured consume pipeline.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var scopeProvider = new ConsumeScopeProvider(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeMessageFilter<TMessage>(scopeProvider);
        var specification = new FilterPipeSpecification<ConsumeContext<TMessage>>(scopeFilter);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds a message scope filter to a configured batch-consumer pipeline.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, IMessageBatch<TMessage>> configurator)
    {
        if (!(configurator is IConsumerMessageSpecification<TConsumer, IMessageBatch<TMessage>> consumerSpecification))
            throw new ArgumentException("The configurator must be a consumer specification");

        var scopeProvider = new ConsumeScopeProvider(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeMessageFilter<IMessageBatch<TMessage>>(scopeProvider);
        var specification = new FilterPipeSpecification<ConsumeContext<IMessageBatch<TMessage>>>(scopeFilter);

        consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Adds an execution scope filter to a configured activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
    {
        var scopeProvider = new ExecuteScopeProvider<TArguments>(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeExecuteFilter<TArguments>(scopeProvider);
        var specification = new FilterPipeSpecification<ExecuteContext<TArguments>>(scopeFilter);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Adds an execution scope filter to a configured execute-activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        var scopeProvider = new ExecuteScopeProvider<TArguments>(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeExecuteFilter<TArguments>(scopeProvider);
        var specification = new FilterPipeSpecification<ExecuteContext<TArguments>>(scopeFilter);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Adds a compensation scope filter to a configured compensate-activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
    {
        var scopeProvider = new CompensateScopeProvider<TLog>(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeCompensateFilter<TLog>(scopeProvider);
        var specification = new FilterPipeSpecification<CompensateContext<TLog>>(scopeFilter);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }
}
