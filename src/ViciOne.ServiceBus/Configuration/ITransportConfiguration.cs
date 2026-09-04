namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for transport configuration.
/// </summary>
public interface ITransportConfiguration :
    ISpecification
{
    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    ITransportConfigurator Configurator { get; }

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    int PrefetchCount { get; }

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    int? ConcurrentMessageLimit { get; }

    /// <summary>
    /// Gets concurrent message limit.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    int GetConcurrentMessageLimit();
}
