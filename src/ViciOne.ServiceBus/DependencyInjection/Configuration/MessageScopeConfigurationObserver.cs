using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public class MessageScopeConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly IServiceProvider _serviceProvider;
    readonly ISetScopedConsumeContext _setScopedConsumeContext;

    /// <summary>
    /// A registration context carries the setter that belongs to its own bus, so it is passed on
    /// rather than replaced by the process default. Routing this overload through the default meant
    /// the retained IRegistrationContext path pushed the consume context into whatever provider the
    /// scope happened to resolve, which is the wrong one as soon as more than one bus is registered.
    /// </summary>
    public MessageScopeConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator, IRegistrationContext context)
        : this(receiveEndpointConfigurator, context,
            context as ISetScopedConsumeContext ?? throw new ArgumentException(
                "The registration context does not carry a scoped consume context setter", nameof(context)))
    {
    }

    public MessageScopeConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator, IServiceProvider serviceProvider,
        ISetScopedConsumeContext setScopedConsumeContext)
        : base(receiveEndpointConfigurator)
    {
        _serviceProvider = serviceProvider;
        _setScopedConsumeContext = setScopedConsumeContext;

        Connect(this);
    }

    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var scopeProvider = new ConsumeScopeProvider(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeMessageFilter<TMessage>(scopeProvider);
        var specification = new FilterPipeSpecification<ConsumeContext<TMessage>>(scopeFilter);

        configurator.AddPipeSpecification(specification);
    }

    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
    {
        if (!(configurator is IConsumerMessageSpecification<TConsumer, Batch<TMessage>> consumerSpecification))
            throw new ArgumentException("The configurator must be a consumer specification");

        var scopeProvider = new ConsumeScopeProvider(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeMessageFilter<Batch<TMessage>>(scopeProvider);
        var specification = new FilterPipeSpecification<ConsumeContext<Batch<TMessage>>>(scopeFilter);

        consumerSpecification.AddPipeSpecification(specification);
    }

    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
    {
        var scopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeExecuteFilter<TActivity, TArguments>(scopeProvider);
        var specification = new FilterPipeSpecification<ExecuteContext<TArguments>>(scopeFilter);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
    {
        var scopeProvider = new ExecuteActivityScopeProvider<TActivity, TArguments>(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeExecuteFilter<TActivity, TArguments>(scopeProvider);
        var specification = new FilterPipeSpecification<ExecuteContext<TArguments>>(scopeFilter);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
    {
        var scopeProvider = new CompensateActivityScopeProvider<TActivity, TLog>(_serviceProvider, _setScopedConsumeContext);
        var scopeFilter = new ScopeCompensateFilter<TActivity, TLog>(scopeProvider);
        var specification = new FilterPipeSpecification<CompensateContext<TLog>>(scopeFilter);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }
}
