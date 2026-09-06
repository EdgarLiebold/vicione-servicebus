namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Describes an ActiveMQ consumer subscription between a topic and queue.</summary>
public interface Consumer
{
    /// <summary>Gets the source topic.</summary>
    Topic Source { get; }

    /// <summary>Gets the consumer queue, or <see langword="null" /> for a direct topic subscription.</summary>
    Queue? Destination { get; }

    /// <summary>Gets the provider selector applied to the subscription.</summary>
    string? Selector { get; }

    /// <summary>Gets the native subscription name, when configured.</summary>
    string? ConsumerName { get; }

    /// <summary>Gets whether multiple consumers share the named topic subscription.</summary>
    /// <remarks>
    /// A shared named subscription lets the broker distribute messages among multiple consumers of the same topic.
    /// </remarks>
    bool IsShared { get; }
}
