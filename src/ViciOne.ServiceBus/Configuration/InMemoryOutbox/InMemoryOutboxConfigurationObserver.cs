using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes in memory outbox configuration events.</summary>
public class InMemoryOutboxConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly Action<IOutboxConfigurator>? _configure;
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public InMemoryOutboxConfigurationObserver(IRegistrationContext context, IConsumePipeConfigurator configurator, Action<IOutboxConfigurator>? configure)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)), configurator, configure)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="setter">The setter.</param>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public InMemoryOutboxConfigurationObserver(ISetScopedConsumeContext? setter, IConsumePipeConfigurator configurator,
        Action<IOutboxConfigurator>? configure)
        : base(configurator)
    {
        _setter = setter;
        _configure = configure;

        Connect(this);
    }

    /// <summary>Reports that message has been configured.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var specification = new InMemoryOutboxSpecification<TMessage>(_setter);

        _configure?.Invoke(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Reports that batch consumer has been configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
    {
        var specification = new InMemoryOutboxSpecification<TMessage>.Batch(_setter);

        _configure?.Invoke(specification);

        configurator.Message(m => m.AddPipeSpecification(specification));
    }

    /// <summary>Reports that activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
    {
        var specification = new InMemoryExecuteContextOutboxSpecification<TArguments>(_setter);

        _configure?.Invoke(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Reports that execute activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        var specification = new InMemoryExecuteContextOutboxSpecification<TArguments>(_setter);

        _configure?.Invoke(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Reports that compensate activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
    {
        var specification = new InMemoryCompensateContextOutboxSpecification<TLog>(_setter);

        _configure?.Invoke(specification);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }
}
