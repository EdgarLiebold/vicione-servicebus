namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for in memory host configurator.
/// </summary>
public interface IInMemoryHostConfigurator
{
    /// <summary>
    /// Maximum number of immediately buffered messages per queue and, independently, scheduled
    /// deliveries waiting for their due time. Producers wait at the boundary instead of allowing
    /// process memory to grow without a limit.
    /// </summary>
    int QueueCapacity { set; }
}
