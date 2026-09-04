using System;
using System.Threading;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

public class MessageRetryConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly CancellationToken _cancellationToken;
    readonly Action<IRetryConfigurator> _configure;

    public MessageRetryConfigurationObserver(IConsumePipeConfigurator receiveEndpointConfigurator, CancellationToken cancellationToken,
        Action<IRetryConfigurator> configure)
        : base(receiveEndpointConfigurator ?? throw new ArgumentNullException(nameof(receiveEndpointConfigurator)))
    {
        ArgumentNullException.ThrowIfNull(configure);

        _cancellationToken = cancellationToken;
        _configure = configure;

        Connect(this);
    }

    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<TMessage>, RetryConsumeContext<TMessage>>(Factory, _cancellationToken);

        _configure(specification);

        configurator.AddPipeSpecification(specification);
    }

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

    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
    {
        var specification = new ExecuteContextRetryPipeSpecification<TArguments>(_cancellationToken);

        _configure(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
    {
        var specification = new ExecuteContextRetryPipeSpecification<TArguments>(_cancellationToken);

        _configure(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
    {
        var specification = new CompensateContextRetryPipeSpecification<TLog>(_cancellationToken);

        _configure(specification);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }

    static RetryConsumeContext<TMessage> Factory<TMessage>(ConsumeContext<TMessage> context, IRetryPolicy retryPolicy, RetryContext retryContext)
        where TMessage : class
    {
        return new RetryConsumeContext<TMessage>(context, retryPolicy, retryContext);
    }
}
