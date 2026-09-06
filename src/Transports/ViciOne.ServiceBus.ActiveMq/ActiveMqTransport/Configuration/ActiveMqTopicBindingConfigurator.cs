using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Configures an ActiveMQ topic binding and its optional message selector.</summary>
public class ActiveMqTopicBindingConfigurator :
    ActiveMqTopicConfigurator,
    IActiveMqTopicBindingConfigurator
{
    /// <summary>Creates a topic-binding configurator from explicit entity settings.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="durable">Whether the topic persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the topic when it is no longer used.</param>
    /// <param name="selector">An optional Apache NMS message selector.</param>
    public ActiveMqTopicBindingConfigurator(string topicName, bool durable = true, bool autoDelete = false, string? selector = null)
        : base(topicName, durable, autoDelete)
    {
        Selector = selector;
    }

    /// <summary>Creates a topic-binding configurator from existing topic settings.</summary>
    /// <param name="topic">The topic settings to copy.</param>
    /// <param name="selector">An optional Apache NMS message selector.</param>
    public ActiveMqTopicBindingConfigurator(Topic topic, string? selector = null)
        : base(topic)
    {
        Selector = selector;
    }

    /// <summary>Gets or sets the Apache NMS message selector applied by the binding.</summary>
    public string? Selector { get; set; }
}
