using System;
using System.Threading;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes message retry configuration events.</summary>
public class MessageRetryConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly CancellationToken _cancellationToken;
    readonly Action<IRetryConfigurator> _configure;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="receiveEndpointConfigurator">The receive endpoint configurator.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public MessageRetryConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator, CancellationToken cancellationToken,
        Action<IRetryConfigurator> configure)
        : base(receiveEndpointConfigurator ?? throw new ArgumentNullException(nameof(receiveEndpointConfigurator)))
    {
        ArgumentNullException.ThrowIfNull(configure);

        _cancellationToken = cancellationToken;
        _configure = configure;

        Connect(this);
    }

    /// <summary>Reports that message has been configured.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<TMessage>, RetryConsumeContext<TMessage>>(Factory, _cancellationToken);

        _configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Reports that batch consumer has been configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
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

    /// <summary>Reports that activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
    {
        var specification = new ExecuteContextRetryPipeSpecification<TArguments>(_cancellationToken);

        _configure(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Reports that execute activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        var specification = new ExecuteContextRetryPipeSpecification<TArguments>(_cancellationToken);

        _configure(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Reports that compensate activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
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
