using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures sql consume topology.</summary>
public interface ISqlConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    ISqlConsumeTopology
{
    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    new ISqlMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Adds specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    void AddSpecification(ISqlConsumeTopologySpecification specification);

    /// <summary>Subscribes the receive queue to a topic using the supplied configurator.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    void Subscribe(string topicName, Action<ISqlTopicSubscriptionConfigurator>? configure = null);
}
