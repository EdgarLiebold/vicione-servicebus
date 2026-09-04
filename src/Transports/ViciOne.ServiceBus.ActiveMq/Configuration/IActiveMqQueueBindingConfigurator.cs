namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq queue binding configurator.
/// </summary>
public interface IActiveMqQueueBindingConfigurator :
    IActiveMqQueueConfigurator
{
    /// <summary>
    /// A routing key for the exchange binding
    /// </summary>
    string? Selector { set; }
}
