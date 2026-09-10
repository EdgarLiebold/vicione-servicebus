using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures sql topic to topic binding.</summary>
public interface ISqlTopicToTopicBindingConfigurator :
    ISqlTopicSubscriptionConfigurator
{
    /// <summary>Creates a subscription between two topics.</summary>
    /// <param name="topicName">The destination topic name.</param>
    /// <param name="configure">The callback that configures the topic subscription.</param>
    void Subscribe(string topicName, Action<ISqlTopicToTopicBindingConfigurator>? configure = null);
}
