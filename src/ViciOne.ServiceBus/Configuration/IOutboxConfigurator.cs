namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures outbox.</summary>
public interface IOutboxConfigurator
{
    /// <summary>Gets or sets the concurrent message delivery.</summary>
    bool ConcurrentMessageDelivery { set; }
}
