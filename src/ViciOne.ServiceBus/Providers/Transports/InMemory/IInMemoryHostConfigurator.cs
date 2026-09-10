namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures capacity limits for an in-memory transport host.</summary>
public interface IInMemoryHostConfigurator
{
    /// <summary>
    /// Sets the positive maximum number of immediately buffered messages per queue and,
    /// independently, delayed deliveries waiting for their due time. Producers wait when either
    /// capacity boundary is reached.
    /// </summary>
    int QueueCapacity { set; }
}
