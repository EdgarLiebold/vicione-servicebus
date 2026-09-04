using System;
using System.Threading;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a message retry configuration observer implementation.
/// </summary>
public class MessageRetryConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly CancellationToken _cancellationToken;
    readonly Action<IRetryConfigurator> _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="receiveEndpointConfigurator">The receive endpoint configurator value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="configure">The configuration callback.</param>
    public MessageRetryConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator, CancellationToken cancellationToken,
        Action<IRetryConfigurator> configure)
        : base(receiveEndpointConfigurator ?? throw new ArgumentNullException(nameof(receiveEndpointConfigurator)))
    {
        ArgumentNullException.ThrowIfNull(configure);

        _cancellationToken = cancellationToken;
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
        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<TMessage>, RetryConsumeContext<TMessage>>(Factory, _cancellationToken);

        _configure(specification);

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
        var consumerSpecification = configurator as IConsumerMessageSpecification<TConsumer, Batch<TMessage>>;
        if (consumerSpecification == null)
            throw new ArgumentException("The configurator must be a consumer specification");

        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<Batch<TMessage>>, RetryConsumeContext<Batch<TMessage>>>(Factory,
            _cancellationToken);

        _configure(specification);

        consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Performs the activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
    {
        var specification = new ExecuteContextRetryPipeSpecification<TArguments>(_cancellationToken);

        _configure(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
    {
        var specification = new ExecuteContextRetryPipeSpecification<TArguments>(_cancellationToken);

        _configure(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
    {
        var specification = new CompensateContextRetryPipeSpecification<TLog>(_cancellationToken);

        _configure(specification);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }

    static RetryConsumeContext<TMessage> Factory<TMessage>(ConsumeContext<TMessage> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        where TMessage : class
    {
        return new RetryConsumeContext<TMessage>(context, retryPolicy, retryContext);
    }
}
