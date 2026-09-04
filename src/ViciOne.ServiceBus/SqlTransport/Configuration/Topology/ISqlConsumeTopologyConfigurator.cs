using System;
using System.ComponentModel;
using ViciOne.ServiceBus.SqlTransport.Configuration;

#nullable enable
namespace ViciOne.ServiceBus;

public interface ISqlConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    ISqlConsumeTopology
{
    new ISqlMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    [EditorBrowsable(EditorBrowsableState.Never)]
    void AddSpecification(ISqlConsumeTopologySpecification specification);

    /// <summary>
    /// Bind an exchange, using the configurator
    /// </summary>
    /// <param name="topicName"></param>
    /// <param name="configure"></param>
    void Subscribe(string topicName, Action<ISqlTopicSubscriptionConfigurator>? configure = null);
}
