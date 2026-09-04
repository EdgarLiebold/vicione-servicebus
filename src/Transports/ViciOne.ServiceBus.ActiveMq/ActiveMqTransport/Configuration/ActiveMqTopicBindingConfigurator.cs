using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq topic binding configurator implementation.
/// </summary>
public class ActiveMqTopicBindingConfigurator :
    ActiveMqTopicConfigurator,
    IActiveMqTopicBindingConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="selector">The selector value.</param>
    public ActiveMqTopicBindingConfigurator(string topicName, bool durable = true, bool autoDelete = false, string? selector = null)
        : base(topicName, durable, autoDelete)
    {
        Selector = selector;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="selector">The selector value.</param>
    public ActiveMqTopicBindingConfigurator(Topic topic, string? selector = null)
        : base(topic)
    {
        Selector = selector;
    }

    /// <summary>
    /// Gets or sets the selector value.
    /// </summary>
    public string? Selector { get; set; }
}
