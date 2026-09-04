namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for transport configurator.
/// </summary>
public interface ITransportConfigurator
{
    /// <summary>
    /// Gets or sets the prefetch count value.
    /// </summary>
    int PrefetchCount { set; }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    int? ConcurrentMessageLimit { set; }
}
