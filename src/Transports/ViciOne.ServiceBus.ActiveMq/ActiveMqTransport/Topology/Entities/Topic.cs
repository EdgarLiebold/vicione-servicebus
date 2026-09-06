namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Describes an ActiveMQ topic declaration.</summary>
public interface Topic
{
    /// <summary>Gets the topic name.</summary>
    string EntityName { get; }

    /// <summary>Gets whether the topic persists across broker restarts.</summary>
    bool Durable { get; }

    /// <summary>Gets whether the broker removes the topic when its owning connection closes.</summary>
    bool AutoDelete { get; }
}
