using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Converts consumer, saga, handler, and activity configuration notifications into one notification
/// for each distinct message type on a consume pipeline.
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

    /// <summary>Connects the observer to every component family supported by the consume pipeline.</summary>
    /// <param name="configurator">The consume pipeline whose message types are observed.</param>
    protected ConfigurationObserver(IConsumePipeConfigurator configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));

        _messageTypes = new HashSet<Type>();

        configurator.ConnectConsumerConfigurationObserver(this);
        configurator.ConnectSagaConfigurationObserver(this);
        configurator.ConnectHandlerConfigurationObserver(this);
        configurator.ConnectActivityConfigurationObserver(this);
    }

    /// <summary>Notifies message observers about a routing-slip activity's execute message.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execute-argument type.</typeparam>
    /// <param name="configurator">The execute pipeline that exposes the message type.</param>
    /// <param name="compensateAddress">The address used for later compensation.</param>
    public virtual void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
        Uri compensateAddress)
        where TActivity : class
        where TArguments : class
    {
        NotifyObserver(configurator.MessageType);
    }

    /// <summary>Notifies message observers about an execute-only activity's message.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execute-argument type.</typeparam>
    /// <param name="configurator">The execute pipeline that exposes the message type.</param>
    public virtual void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
        where TActivity : class
        where TArguments : class
    {
        NotifyObserver(configurator.MessageType);
    }

    /// <summary>Notifies message observers about an activity's compensation message.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TLog">The compensation-log type.</typeparam>
    /// <param name="configurator">The compensation pipeline that exposes the message type.</param>
    public virtual void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
        where TActivity : class
        where TLog : class
    {
        NotifyObserver(configurator.MessageType);
    }

    void IConsumerConfigurationObserver.ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
    {
        // Message notifications own per-message middleware; the consumer-level notification adds no message type.
    }

    void IConsumerConfigurationObserver.ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
    {
        if (typeof(TMessage).TryGetSingleClosedGenericArguments(typeof(IMessageBatch<>), out Type[] types))
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
        // Message notifications own per-message middleware; the saga-level notification adds no message type.
    }

    /// <summary>Leaves state-machine metadata unchanged because its message notifications are observed separately.</summary>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="configurator">The configured saga.</param>
    /// <param name="stateMachine">The state-machine definition.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
    }

    void ISagaConfigurationObserver.SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
    {
        NotifyObserver<TMessage>();
    }

    /// <summary>Allows derived observers to configure a batch pipeline without treating its element type as a separate delivery.</summary>
    /// <typeparam name="TConsumer">The batch consumer implementation.</typeparam>
    /// <typeparam name="TMessage">The message type contained by the batch.</typeparam>
    /// <param name="configurator">The configured batch-message pipeline.</param>
    public virtual void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, IMessageBatch<TMessage>> configurator)
        where TConsumer : class, IConsumer<IMessageBatch<TMessage>>
        where TMessage : class
    {
        // The default observer has no batch-specific middleware; derived observers can override this hook.
    }

    void NotifyObserver<TMessage>()
        where TMessage : class
    {
        if (!_messageTypes.Add(typeof(TMessage)))
            return;

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
