namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Creates de-duplicated ActiveMQ topic, queue, and consumer declarations.</summary>
public interface IBrokerTopologyBuilder
{
    /// <summary>Declares a topic.</summary>
    /// <param name="name">The topic name.</param>
    /// <param name="durable">Whether the topic persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the topic when it is no longer used.</param>
    /// <returns>A handle for the de-duplicated topic.</returns>
    TopicHandle CreateTopic(string name, bool durable, bool autoDelete);

    /// <summary>Declares a queue.</summary>
    /// <param name="name">The queue name.</param>
    /// <param name="durable">Whether the queue persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the queue when it is no longer used.</param>
    /// <returns>A handle for the de-duplicated queue.</returns>
    QueueHandle CreateQueue(string name, bool durable, bool autoDelete);

    /// <summary>Creates a consumer binding from a topic to a queue or named topic subscription.</summary>
    /// <param name="topic">The source topic handle.</param>
    /// <param name="queue">The destination queue, or <see langword="null" /> for direct topic consumption.</param>
    /// <param name="selector">An optional Apache NMS message selector.</param>
    /// <param name="consumerName">An optional native subscription name.</param>
    /// <param name="shared">Whether the named topic subscription is shared.</param>
    /// <returns>A handle for the de-duplicated consumer binding.</returns>
    ConsumerHandle BindConsumer(TopicHandle topic, QueueHandle? queue, string? selector, string? consumerName = null, bool shared = false);
}
