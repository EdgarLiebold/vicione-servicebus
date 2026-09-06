namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures an ActiveMQ topic.</summary>
public interface IActiveMqTopicConfigurator
{
    /// <summary>Sets whether the topic persists across broker restarts.</summary>
    bool Durable { set; }

    /// <summary>Sets whether the broker removes the topic when its owning connection closes.</summary>
    bool AutoDelete { set; }
}
