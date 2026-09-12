using System;

namespace ViciOne.ServiceBus.Configuration;

internal sealed class TimeoutConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly Action<ITimeoutConfigurator> _configure;

    public TimeoutConfigurationObserver(IConsumePipeConfigurator configurator, Action<ITimeoutConfigurator> configure)
        : base(configurator)
    {
        _configure = configure;

        Connect(this);
    }

    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var specification = new TimeoutSpecification<TMessage>();

        _configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, IMessageBatch<TMessage>> configurator)
    {
        var specification = new TimeoutSpecification<IMessageBatch<TMessage>>();

        _configure(specification);

        configurator.Message(m => m.AddPipeSpecification(specification));
    }

    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
    {
        var specification = new ExecuteContextTimeoutSpecification<TArguments>();

        _configure(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        var specification = new ExecuteContextTimeoutSpecification<TArguments>();

        _configure(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
    {
        var specification = new CompensateContextTimeoutSpecification<TLog>();

        _configure(specification);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }
}
