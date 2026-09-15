using System;
using System.Threading;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds retry specifications to configured message, batch, and activity pipelines.</summary>
internal sealed class MessageRetryConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly CancellationToken _cancellationToken;
    readonly Action<IRetryConfigurator> _configure;

    /// <summary>Creates an observer for a consume pipeline and a shared retry policy callback.</summary>
    /// <param name="receiveEndpointConfigurator">The consume pipeline whose configurations are observed.</param>
    /// <param name="cancellationToken">The token observed while retry delays are pending.</param>
    /// <param name="configure">The callback applied to each retry policy.</param>
    MessageRetryConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator, CancellationToken cancellationToken,
        Action<IRetryConfigurator> configure)
        : base(receiveEndpointConfigurator ?? throw new ArgumentNullException(nameof(receiveEndpointConfigurator)))
    {
        ArgumentNullException.ThrowIfNull(configure);

        _cancellationToken = cancellationToken;
        _configure = configure;
    }

    /// <summary>Attaches retry configuration to all current and future message pipelines.</summary>
    /// <param name="configurator">The consume pipeline whose component configurations are observed.</param>
    /// <param name="cancellationToken">The token observed while retry delays are pending.</param>
    /// <param name="configure">The callback applied to each retry policy.</param>
    public static void Attach(IConsumePipeConfigurator configurator, CancellationToken cancellationToken,
        Action<IRetryConfigurator> configure)
    {
        var observer = new MessageRetryConfigurationObserver(configurator, cancellationToken, configure);
        observer.Connect(observer);
    }

    /// <summary>Adds retry handling to a configured message pipeline.</summary>
    /// <typeparam name="TMessage">The configured message contract.</typeparam>
    /// <param name="configurator">The consume pipeline that owns the message pipeline.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<TMessage>, RetryConsumeContext<TMessage>>(Factory, _cancellationToken);

        _configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds retry handling around a configured batch-consumer invocation.</summary>
    /// <typeparam name="TConsumer">The batch consumer implementation.</typeparam>
    /// <typeparam name="TMessage">The message contract contained by the batch.</typeparam>
    /// <param name="configurator">The configured batch-consumer pipeline.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, IMessageBatch<TMessage>> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (configurator is not IConsumerMessageSpecification<TConsumer, IMessageBatch<TMessage>> consumerSpecification)
            throw new ArgumentException("The configurator must implement the consumer message specification contract.", nameof(configurator));

        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<IMessageBatch<TMessage>>, RetryConsumeContext<IMessageBatch<TMessage>>>(Factory,
            _cancellationToken);

        _configure(specification);

        consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Adds retry handling to a configured activity execution pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The activity execution pipeline.</param>
    /// <param name="compensateAddress">The address used if the activity is later compensated.</param>
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(compensateAddress);

        var specification = new ExecuteContextRetryPipeSpecification<TArguments>(_cancellationToken);

        _configure(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Adds retry handling to a configured execute-activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The execute-only activity pipeline.</param>
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var specification = new ExecuteContextRetryPipeSpecification<TArguments>(_cancellationToken);

        _configure(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Adds retry handling to a configured compensate-activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The activity compensation pipeline.</param>
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

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
