namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures outbox.</summary>
public interface IOutboxConfigurator
{
    /// <summary>Sets whether independent buffered operations may be delivered concurrently.</summary>
    bool ConcurrentMessageDelivery { set; }
}
