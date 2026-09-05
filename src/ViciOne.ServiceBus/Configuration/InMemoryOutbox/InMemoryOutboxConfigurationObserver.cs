using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an in memory outbox configuration observer implementation.
/// </summary>
public class InMemoryOutboxConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly Action<IOutboxConfigurator>? _configure;
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public InMemoryOutboxConfigurationObserver(IRegistrationContext context, IConsumePipeConfigurator configurator, Action<IOutboxConfigurator>? configure)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)), configurator, configure)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="setter">The setter value.</param>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public InMemoryOutboxConfigurationObserver(ISetScopedConsumeContext? setter, IConsumePipeConfigurator configurator,
        Action<IOutboxConfigurator>? configure)
        : base(configurator)
    {
        _setter = setter;
        _configure = configure;

        Connect(this);
    }

    /// <summary>
    /// Performs the message configured operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var specification = new InMemoryOutboxSpecification<TMessage>(_setter);

        _configure?.Invoke(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Performs the batch consumer configured operation.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
    {
        var specification = new InMemoryOutboxSpecification<TMessage>.Batch(_setter);

        _configure?.Invoke(specification);

        configurator.Message(m => m.AddPipeSpecification(specification));
    }

    /// <summary>
    /// Performs the activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
    {
        var specification = new InMemoryExecuteContextOutboxSpecification<TArguments>(_setter);

        _configure?.Invoke(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        var specification = new InMemoryExecuteContextOutboxSpecification<TArguments>(_setter);

        _configure?.Invoke(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
    {
        var specification = new InMemoryCompensateContextOutboxSpecification<TLog>(_setter);

        _configure?.Invoke(specification);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }
}
