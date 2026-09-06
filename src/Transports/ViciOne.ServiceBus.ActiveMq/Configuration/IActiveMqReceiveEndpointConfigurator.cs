using System;
using ViciOne.ServiceBus.ActiveMq;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures an ActiveMQ queue receive endpoint and its topic bindings.</summary>
public interface IActiveMqReceiveEndpointConfigurator :
    IReceiveEndpointConfigurator,
    IActiveMqQueueEndpointConfigurator
{
    /// <summary>Binds the message topic to the receive endpoint.</summary>
    /// <typeparam name="T">The message type whose publish topic is bound.</typeparam>
    /// <param name="callback">An optional callback that configures the topic binding.</param>
    void Bind<T>(Action<IActiveMqTopicBindingConfigurator>? callback = null)
        where T : class;

    /// <summary>Binds the named topic to the receive endpoint.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="callback">The callback that configures the topic subscription.</param>
    void Bind(string topicName, Action<IActiveMqTopicBindingConfigurator>? callback = null);

    /// <summary>Configures the Apache NMS session pipeline used by the endpoint.</summary>
    /// <param name="configure">The callback that configures the session pipeline.</param>
    void ConfigureSession(Action<IPipeConfigurator<SessionContext>> configure);
}
