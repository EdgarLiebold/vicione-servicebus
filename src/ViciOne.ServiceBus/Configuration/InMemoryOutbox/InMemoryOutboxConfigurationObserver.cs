using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds in-memory outbox specifications to configured message and activity pipelines.</summary>
public class InMemoryOutboxConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly Action<IOutboxConfigurator>? _configure;
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>Creates an observer that obtains the scoped consume-context accessor from a registration context.</summary>
    /// <param name="context">The registration context that supplies the scoped consume-context accessor.</param>
    /// <param name="configurator">The consume pipeline whose configurations are observed.</param>
    /// <param name="configure">The optional callback applied to each outbox specification.</param>
    public InMemoryOutboxConfigurationObserver(IRegistrationContext context, IConsumePipeConfigurator configurator, Action<IOutboxConfigurator>? configure)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)), configurator, configure)
    {
    }

    /// <summary>Creates an observer with an optional scoped consume-context accessor.</summary>
    /// <param name="setter">The accessor used to expose the active context inside a dependency-injection scope.</param>
    /// <param name="configurator">The consume pipeline whose configurations are observed.</param>
    /// <param name="configure">The optional callback applied to each outbox specification.</param>
    public InMemoryOutboxConfigurationObserver(ISetScopedConsumeContext? setter, IConsumePipeConfigurator configurator,
        Action<IOutboxConfigurator>? configure)
        : base(configurator)
    {
        _setter = setter;
        _configure = configure;

        Connect(this);
    }

    /// <summary>Adds an in-memory outbox to a configured message pipeline.</summary>
    /// <typeparam name="TMessage">The configured message contract.</typeparam>
    /// <param name="configurator">The consume pipeline that owns the message pipeline.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var specification = new InMemoryOutboxSpecification<TMessage>(_setter);

        _configure?.Invoke(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds per-message in-memory outboxes to a configured batch-consumer pipeline.</summary>
    /// <typeparam name="TConsumer">The batch consumer implementation.</typeparam>
    /// <typeparam name="TMessage">The message contract contained by the batch.</typeparam>
    /// <param name="configurator">The configured batch-consumer pipeline.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, IMessageBatch<TMessage>> configurator)
    {
        var specification = new InMemoryOutboxSpecification<TMessage>.BatchSpecification(_setter);

        _configure?.Invoke(specification);

        configurator.Message(m => m.AddPipeSpecification(specification));
    }

    /// <summary>Adds an in-memory outbox to a configured activity execution pipeline.</summary>
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

    /// <summary>Adds an in-memory outbox to a configured execute-activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        var specification = new InMemoryExecuteContextOutboxSpecification<TArguments>(_setter);

        _configure?.Invoke(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Adds an in-memory outbox to a configured compensate-activity pipeline.</summary>
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
