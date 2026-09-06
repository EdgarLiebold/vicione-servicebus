namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures an ActiveMQ queue binding and its message selector.</summary>
public interface IActiveMqQueueBindingConfigurator :
    IActiveMqQueueConfigurator
{
    /// <summary>Sets the Apache NMS message selector applied to the queue consumer.</summary>
    string? Selector { set; }
}
