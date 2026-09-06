namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Defines the name and lifecycle of an ActiveMQ queue or topic.</summary>
public interface EntitySettings
{
    /// <summary>Gets the queue or topic name.</summary>
    string EntityName { get; }

    /// <summary>Gets whether the entity persists across broker restarts.</summary>
    bool Durable { get; }

    /// <summary>Gets whether the broker deletes the entity when its owning connection closes.</summary>
    bool AutoDelete { get; }
}
