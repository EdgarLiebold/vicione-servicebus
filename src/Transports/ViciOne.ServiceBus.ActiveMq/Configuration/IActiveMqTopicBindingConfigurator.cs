namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures an ActiveMQ topic subscription.</summary>
public interface IActiveMqTopicBindingConfigurator :
    IActiveMqTopicConfigurator
{
    /// <summary>Sets the Apache NMS message selector applied to the topic subscription.</summary>
    string? Selector { set; }
}
