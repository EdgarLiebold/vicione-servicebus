using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Combines the separate configuration observers into a single observer that is for each message type, called once, to configure each
/// message pipeline only once. Only outputs the individual message events for configuring the pipeline.
/// </summary>
public class ConfigurationObserver :
    Connectable<IMessageConfigurationObserver>,
    IConsumerConfigurationObserver,
    ISagaConfigurationObserver,
    IHandlerConfigurationObserver,
    IActivityConfigurationObserver
{
    readonly IConsumePipeConfigurator _configurator;
    readonly HashSet<Type> _messageTypes;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    protected ConfigurationObserver(IConsumePipeConfigurator configurator)
    {
        _configurator = configurator;

        _messageTypes = new HashSet<Type>();

        configurator.ConnectConsumerConfigurationObserver(this);
        configurator.ConnectSagaConfigurationObserver(this);
        configurator.ConnectHandlerConfigurationObserver(this);
        configurator.ConnectActivityConfigurationObserver(this);
    }

    /// <summary>
    /// Performs the activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    public virtual void ActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        NotifyObserver<RoutingSlip>();
    }

    /// <summary>
    /// Performs the execute activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public virtual void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityConfigurator<TActivity, TArguments> configurator)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        NotifyObserver<RoutingSlip>();
    }

    /// <summary>
    /// Performs the compensate activity configured operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public virtual void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityConfigurator<TActivity, TLog> configurator)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        NotifyObserver<RoutingSlip>();
    }

    void IConsumerConfigurationObserver.ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
    {
    }

    void IConsumerConfigurationObserver.ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
    {
        if (typeof(TMessage).TryGetSingleClosedGenericArguments(typeof(Batch<>), out Type[] types))
        {
            var method = typeof(ConfigurationObserver)
                .GetMethod(nameof(BatchConsumerConfigured))
                ?? throw new InvalidOperationException($"The {nameof(BatchConsumerConfigured)} method was not found.");

            method
                .MakeGenericMethod(typeof(TConsumer), types[0])
                .Invoke(this, new object[] { configurator });
        }
        else
            NotifyObserver<TMessage>();
    }

    void IHandlerConfigurationObserver.HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
    {
        NotifyObserver<TMessage>();
    }

    void ISagaConfigurationObserver.SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
    {
    }

    /// <summary>
    /// Performs the state machine saga configured operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="stateMachine">The state machine value.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, SagaStateMachine<TInstance> stateMachine)
        where TInstance : class, ISaga, SagaStateMachineInstance
    {
    }

    void ISagaConfigurationObserver.SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
    {
        NotifyObserver<TMessage>();
    }

    /// <summary>
    /// Performs the batch consumer configured operation.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public virtual void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
        where TConsumer : class, IConsumer<Batch<TMessage>>
        where TMessage : class
    {
    }

    void NotifyObserver<TMessage>()
        where TMessage : class
    {
        if (_messageTypes.Contains(typeof(TMessage)))
            return;

        _messageTypes.Add(typeof(TMessage));

        ForEach(observer => observer.MessageConfigured<TMessage>(_configurator));
    }
}
