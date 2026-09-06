using System;
using System.Collections.Generic;
using System.Reflection;
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    protected ConfigurationObserver(IConsumePipeConfigurator configurator)
    {
        _configurator = configurator;

        _messageTypes = new HashSet<Type>();

        configurator.ConnectConsumerConfigurationObserver(this);
        configurator.ConnectSagaConfigurationObserver(this);
        configurator.ConnectHandlerConfigurationObserver(this);
        configurator.ConnectActivityConfigurationObserver(this);
    }

    /// <summary>Reports that activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public virtual void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        NotifyObserver(configurator.MessageType);
    }

    /// <summary>Reports that execute activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public virtual void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        NotifyObserver(configurator.MessageType);
    }

    /// <summary>Reports that compensate activity has been configured.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public virtual void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        NotifyObserver(configurator.MessageType);
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

    /// <summary>Reports that state machine saga has been configured.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
    }

    void ISagaConfigurationObserver.SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
    {
        NotifyObserver<TMessage>();
    }

    /// <summary>Reports that batch consumer has been configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
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

    void NotifyObserver(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        MethodInfo method = typeof(ConfigurationObserver).GetMethod(nameof(NotifyObserverByType),
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The {nameof(NotifyObserverByType)} method was not found.");

        method.MakeGenericMethod(messageType).Invoke(this, null);
    }

    void NotifyObserverByType<TMessage>()
        where TMessage : class
    {
        NotifyObserver<TMessage>();
    }
}
