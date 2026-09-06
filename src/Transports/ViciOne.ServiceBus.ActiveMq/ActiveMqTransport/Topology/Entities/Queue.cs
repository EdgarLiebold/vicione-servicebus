namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Defines the name and lifecycle of an ActiveMQ queue.</summary>
public interface Queue
{
    /// <summary>Gets the queue name.</summary>
    string EntityName { get; }

    /// <summary>Gets whether the broker removes the queue when its owning connection closes.</summary>
    bool AutoDelete { get; }

    /// <summary>Gets whether the queue persists across broker restarts.</summary>
    bool Durable { get; }
}
