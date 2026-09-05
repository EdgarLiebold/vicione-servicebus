using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a scheduled redelivery configuration observer implementation.
/// </summary>
public class ScheduledRedeliveryConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly IConsumePipeConfigurator _configurator;
    readonly Action<IRedeliveryConfigurator> _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public ScheduledRedeliveryConfigurationObserver(IConsumePipeConfigurator configurator, Action<IRedeliveryConfigurator> configure)
        : base(configurator)
    {
        _configurator = configurator;
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
        var redeliveryPipeSpecification = AddRedeliveryPipeSpecification<TMessage>(configurator);

        if (typeof(TMessage).IsDefined(typeof(ActivityMessageAttribute), inherit: false))
            return;

        var retrySpecification = new RedeliveryRetryPipeSpecification<TMessage>(redeliveryPipeSpecification);

        _configure?.Invoke(retrySpecification);

        configurator.AddPipeSpecification(retrySpecification);
    }

    /// <summary>
    /// Performs the batch consumer configured operation.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
    {
        MessageConfigured<TMessage>(_configurator);
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
        base.ActivityConfigured(configurator, compensateAddress);

        var specification = new ExecuteContextRedeliveryPipeSpecification<TArguments>();

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
        base.ExecuteActivityConfigured(configurator);

        var specification = new ExecuteContextRedeliveryPipeSpecification<TArguments>();

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
        base.CompensateActivityConfigured(configurator);

        var specification = new CompensateContextRedeliveryPipeSpecification<TLog>();

        _configure?.Invoke(specification);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }

    /// <summary>
    /// Adds redelivery pipe specification to the configuration.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual IRedeliveryPipeSpecification AddRedeliveryPipeSpecification<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var redeliverySpecification = new ScheduledRedeliveryPipeSpecification<TMessage>();

        configurator.AddPipeSpecification(redeliverySpecification);

        return redeliverySpecification;
    }
}
