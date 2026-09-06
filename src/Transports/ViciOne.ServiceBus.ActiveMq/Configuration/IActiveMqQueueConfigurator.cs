namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures an ActiveMQ queue and its consumer subscription.</summary>
public interface IActiveMqQueueConfigurator
{
    /// <summary>Sets whether the queue persists across broker restarts.</summary>
    bool Durable { set; }

    /// <summary>Sets whether the broker removes the queue when its owning connection closes.</summary>
    bool AutoDelete { set; }
}
